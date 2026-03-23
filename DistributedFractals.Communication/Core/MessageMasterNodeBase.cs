using System.Collections.Concurrent;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Core;

public abstract class MessageMasterNodeBase : IMessageMasterNode
{
    private readonly ConcurrentDictionary<Guid, Guid> _workers = new();

    public IReadOnlyCollection<Guid> Workers => _workers.Values.ToList();

    public Guid Identifier { get; } = Guid.NewGuid();
    public event Action<Guid>? WorkerRegistered;
    public event Action<Guid>? WorkerUnregistered;
    public abstract event Action<BaseMessage>? MessageReceived;

    public void RegisterWorker(Guid worker)
    {
        _workers[worker] = worker;
        WorkerRegistered?.Invoke(worker);
    }

    public virtual void UnregisterWorker(Guid worker)
    {
        _workers.TryRemove(worker, out _);
        WorkerUnregistered?.Invoke(worker);
    }

    public abstract Task StartAsync();
    public abstract Task SendToWorkerAsync(Guid workerIdentifier, BaseMessage baseMessage);
    public abstract Task BroadcastAsync(BaseMessage baseMessage);
    public abstract ValueTask DisposeAsync();
}
