using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Core;

public interface IMessageMasterNode : IMessageNode
{
    IReadOnlyCollection<Guid> Workers { get; }

    event Action<Guid>? WorkerRegistered;
    event Action<Guid>? WorkerUnregistered;

    void RegisterWorker(Guid worker);
    void UnregisterWorker(Guid worker);

    Task SendToWorkerAsync(Guid workerIdentifier, BaseMessage baseMessage);
    Task BroadcastAsync(BaseMessage baseMessage);
}
