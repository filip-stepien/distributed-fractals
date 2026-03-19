namespace DistributedFractals.Server.Core;

public interface IMessageNodeFactory
{
    IMessageMasterNode CreateMaster();
    IMessageWorkerNode CreateWorker();
}