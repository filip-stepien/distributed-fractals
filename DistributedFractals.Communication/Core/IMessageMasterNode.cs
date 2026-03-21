using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Core;

public interface IMessageMasterNode : IMessageNode
{
    public event Action<Message>? MessageReceived;
    
    Task SendToWorker(MessageNodeIdentifier workerIdentifier, Message message);
    
    Task BroadcastToWorkers(Message message);
}