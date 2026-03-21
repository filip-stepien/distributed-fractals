using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Core;

public interface IMessageWorkerNode : IMessageNode
{
    public event Action<Message>? MessageReceived;
    
    Task SendToMaster(Message message);
}