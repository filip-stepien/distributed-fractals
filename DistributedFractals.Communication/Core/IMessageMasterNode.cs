using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Core;

public interface IMessageMasterNode : IMessageNode
{
    void RegisterWorker(MessageNodeIdentifier worker);
    void UnregisterWorker(MessageNodeIdentifier worker);

    Task SendToWorkerAsync(MessageNodeIdentifier workerIdentifier, Message message);
    Task BroadcastAsync(Message message);
}
