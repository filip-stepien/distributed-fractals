using DistributedFractals.Fractal.Core;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Handlers;
using DistributedFractals.Server.Heartbeat;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Dispatchers;

public static class MessageDispatcherFactory
{
    public static IMessageDispatcher CreateServer(IHeartbeatMessageServer server, IFrameResultReceiver receiver)
    {
        MessageDispatcher dispatcher = new();

        dispatcher.Register(new JoinMessageHandler(server));
        dispatcher.Register(new HeartbeatMessageHandler(server));
        RenderResultHandler renderResultHandler = new(receiver);
        dispatcher.Register<RenderResultMessage>(renderResultHandler);
        dispatcher.Register<RenderBatchResultMessage>(renderResultHandler);

        return dispatcher;
    }

    public static IMessageDispatcher CreateClient(
        IMessageClient client,
        Action<int> onFrameStarted,
        Action<int, TimeSpan, FractalResult> onFrameCompleted,
        Action<int> onFrameFailed,
        Action onDisconnected
    ) {
        MessageDispatcher dispatcher = new();

        RenderFractalHandler renderFractalHandler = new(
            client,
            onFrameStarted,
            onFrameCompleted,
            onFrameFailed
        );

        dispatcher.Register<RenderFrameMessage>(renderFractalHandler);
        dispatcher.Register<RenderBatchMessage>(renderFractalHandler);

        dispatcher.Register(new UnregisteredMessageHandler(onDisconnected));

        return dispatcher;
    }
}
