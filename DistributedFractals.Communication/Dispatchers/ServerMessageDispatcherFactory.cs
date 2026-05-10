using DistributedFractals.Server.Core;
using DistributedFractals.Server.Handlers;
using DistributedFractals.Server.Heartbeat;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Dispatchers;

public static class ServerMessageDispatcherFactory
{
    public static IMessageDispatcher Create(IHeartbeatMessageServer server, IFrameResultReceiver receiver)
    {
        MessageDispatcher dispatcher = new();

        dispatcher.Register(new JoinMessageHandler(server));
        dispatcher.Register(new HeartbeatMessageHandler(server));

        RenderResultHandler renderResultHandler = new(receiver);
        dispatcher.Register<RenderResultMessage>(renderResultHandler);
        dispatcher.Register<RenderBatchResultMessage>(renderResultHandler);

        return dispatcher;
    }
}
