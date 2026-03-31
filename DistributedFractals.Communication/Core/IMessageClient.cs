using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Core;

public interface IMessageClient : IMessageNode
{
    Task SendToServerAsync(BaseMessage baseMessage);
}
