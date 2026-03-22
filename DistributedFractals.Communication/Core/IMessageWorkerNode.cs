using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Core;

public interface IMessageWorkerNode : IMessageNode
{
    event Action<Message>? MessageReceived;

    Task SendAsync(Message message);
}
