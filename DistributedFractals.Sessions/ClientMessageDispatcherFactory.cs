using DistributedFractals.Core.Colorizers;
using DistributedFractals.Core.Core;
using DistributedFractals.Core.Generators.Mandelbrot;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Dispatching;
using DistributedFractals.Server.Handlers;

namespace DistributedFractals.Sessions;

internal static class ClientMessageDispatcherFactory
{
    public static IMessageDispatcher Create(
        IMessageClient client,
        Action<int> onFrameStarted,
        Action<int, TimeSpan, FractalResult> onFrameCompleted,
        Action<int> onFrameFailed,
        Action onDisconnected
    ) {
        MessageDispatcher dispatcher = new();

        dispatcher.Register(
            new RenderFractalHandler.Builder(client)
                .AddGenerator(FractalGeneratorType.Mandelbrot, new MandelbrotGenerator())
                .AddColorizer(FractalColorizerType.BlackAndWhite, new BlackAndWhiteColorizer())
                .AddColorizer(FractalColorizerType.CyclingHsv, new CyclingHsvColorizer())
                .OnStarted(onFrameStarted)
                .OnCompleted(onFrameCompleted)
                .OnFailed(onFrameFailed)
                .Build()
        );

        dispatcher.Register(new UnregisteredMessageHandler(onDisconnected));

        return dispatcher;
    }
}
