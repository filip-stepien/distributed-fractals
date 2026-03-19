namespace DistributedFractals.Server.Core;

public interface IMessageWorkerNode : IMessageNode
{
    public MessageNodeIdentifier Identifier { get; }
    
    public event Action<MasterNodeMessage>? MessageReceived;
    
    Task SendToMaster(WorkerNodeMessage message);
}