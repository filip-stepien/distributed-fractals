using DistributedFractals.Server.Core;
using DistributedFractals.Server.Handlers.Master;
using DistributedFractals.Server.Handlers.Worker;

namespace DistributedFractals.Server.Dispatching;

public class MessageDispatcherFactory
{
    public IMessageDispatcher CreateMasterDispatcher(IMessageMasterNode master)
    {
        MessageDispatcher dispatcher = new();
        dispatcher.Register(new MasterJoinMessageHandler(master));
        dispatcher.Register(new MasterHeartbeatMessageHandler());
        dispatcher.Register(new MasterTextMessageHandler());
        return dispatcher;
    }

    public IMessageDispatcher CreateWorkerDispatcher()
    {
        MessageDispatcher dispatcher = new();
        dispatcher.Register(new WorkerHeartbeatMessageHandler());
        dispatcher.Register(new WorkerTextMessageHandler());
        return dispatcher;
    }
}
