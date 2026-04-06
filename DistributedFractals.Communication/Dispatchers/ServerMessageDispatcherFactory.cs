using DistributedFractals.Server.Core;
using DistributedFractals.Server.Handlers;
using DistributedFractals.Server.Heartbeat;

namespace DistributedFractals.Server.Dispatchers;

public static class ServerMessageDispatcherFactory
{
    public static IMessageDispatcher Create(IHeartbeatMessageServer server, IFrameResultReceiver receiver)
    {
        MessageDispatcher dispatcher = new();

        dispatcher.Register(new JoinMessageHandler(server));
        dispatcher.Register(new HeartbeatMessageHandler(server));
        dispatcher.Register(new RenderResultHandler(receiver));

        return dispatcher;
    }
}
