namespace DistributedFractals.Server.Core;

public interface IMessageMasterNode : IMessageNode
{
    public event Action<WorkerNodeMessage>? MessageReceived;
    
    Task SendToWorker(MessageNodeIdentifier workerIdentifier, MasterNodeMessage message);
    Task BroadcastToWorkers(MasterNodeMessage message);
}