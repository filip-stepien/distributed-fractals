namespace DistributedFractals.Server.Core;

public interface IMessageNodeFactory
{
    IMessageNode CreateMaster();
    IMessageNode CreateWorker();
}