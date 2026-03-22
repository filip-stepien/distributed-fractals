using DistributedFractals.Server.Core;
using DistributedFractals.Server.Handlers.Master;
using DistributedFractals.Server.Handlers.Worker;

namespace DistributedFractals.Server.Dispatching;

public static class MessageDispatcherFactory
{
    public static IMessageDispatcher CreateMasterDispatcher(IMessageMasterNode master)
    {
        MessageDispatcher dispatcher = new();
        dispatcher.Register(new MasterJoinMessageHandler(master));
        dispatcher.Register(new MasterHeartbeatMessageHandler());
        dispatcher.Register(new MasterTextMessageHandler());
        return dispatcher;
    }

    public static IMessageDispatcher CreateWorkerDispatcher()
    {
        MessageDispatcher dispatcher = new();
        dispatcher.Register(new WorkerHeartbeatMessageHandler());
        dispatcher.Register(new WorkerTextMessageHandler());
        return dispatcher;
    }
}
