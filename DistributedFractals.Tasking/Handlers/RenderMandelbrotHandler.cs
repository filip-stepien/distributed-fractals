using DistributedFractals.Core.Core;
using DistributedFractals.Core.Generators.Mandelbrot;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Handlers;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Tasking.Handlers;

public class RenderMandelbrotHandler(
    MandelbrotGenerator generator,
    IFractalColorizer colorizer,
    IMessageWorkerNode worker) : IMessageHandler<RenderMandelbrotMessage>
{
    public async Task HandleAsync(RenderMandelbrotMessage message)
    {
        FractalResult result = generator.Generate(message.Options, colorizer);
        await worker.SendToMasterAsync(new RenderResultMessage(worker.Identifier, result));
    }
}
