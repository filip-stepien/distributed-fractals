using DistributedFractals.Orchestration.Schedulers;
using DistributedFractals.Server.Dispatching;
using DistributedFractals.Server.Handlers;
using DistributedFractals.Server.Heartbeat;

namespace DistributedFractals.Sessions;

internal static class ServerMessageDispatcherFactory
{
    public static IMessageDispatcher Create(IHeartbeatMessageServer server, IFrameScheduler scheduler)
    {
        MessageDispatcher dispatcher = new();

        dispatcher.Register(new JoinMessageHandler(server));
        dispatcher.Register(new HeartbeatMessageHandler(server));
        dispatcher.Register(new RenderResultHandler(scheduler));

        return dispatcher;
    }
}
