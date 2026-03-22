using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Core;

public interface IMessageMasterNode : IMessageNode
{
    IReadOnlyCollection<MessageNodeIdentifier> ConnectedWorkers { get; }

    event Action<Message>? MessageReceived;

    void RegisterWorker(MessageNodeIdentifier worker);
    void UnregisterWorker(MessageNodeIdentifier worker);

    Task SendToWorkerAsync(MessageNodeIdentifier workerIdentifier, Message message);
    Task BroadcastAsync(Message message);
}
