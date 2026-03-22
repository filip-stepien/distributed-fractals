namespace DistributedFractals.Server.Core;

public interface IMessageNodeFactory
{
    IMessageMasterNode CreateMasterNode();
    IMessageWorkerNode CreateWorkerNode();
}