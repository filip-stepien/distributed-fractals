using DistributedFractals.Fractal.Core;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Handlers;
using DistributedFractals.Server.Heartbeat;

namespace DistributedFractals.Server.Dispatchers;

public static class MessageDispatcherFactory
{
    public static IMessageDispatcher CreateServer(IHeartbeatMessageServer server, IFrameResultReceiver receiver)
    {
        MessageDispatcher dispatcher = new();

        dispatcher.Register(new JoinMessageHandler(server));
        dispatcher.Register(new HeartbeatMessageHandler(server));
        dispatcher.Register(new RenderResultHandler(receiver));

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

        dispatcher.Register(new RenderFractalHandler(
            client,
            onFrameStarted,
            onFrameCompleted,
            onFrameFailed
        ));

        dispatcher.Register(new UnregisteredMessageHandler(onDisconnected));

        return dispatcher;
    }
}
