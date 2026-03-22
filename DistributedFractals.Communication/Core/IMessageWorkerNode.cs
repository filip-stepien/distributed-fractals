using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Core;

public interface IMessageWorkerNode : IMessageNode
{
    Task SendToMasterAsync(Message message);
}
