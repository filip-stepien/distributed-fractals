using System.Collections.Concurrent;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Heartbeat;

public sealed class HeartbeatMasterNode(IMessageMasterNode inner, TimeSpan heartbeatTimeout) : IMessageMasterNode
{
    private readonly ConcurrentDictionary<Guid, HeartbeatTracker> _trackers = new();

    public Guid Identifier => inner.Identifier;
    public IReadOnlyCollection<Guid> Workers => inner.Workers;

    public event Action<BaseMessage>? MessageReceived
    {
        add => inner.MessageReceived += value;
        remove => inner.MessageReceived -= value;
    }

    public event Action<Guid>? WorkerRegistered
    {
        add => inner.WorkerRegistered += value;
        remove => inner.WorkerRegistered -= value;
    }

    public event Action<Guid>? WorkerUnregistered
    {
        add => inner.WorkerUnregistered += value;
        remove => inner.WorkerUnregistered -= value;
    }

    public void RecordHeartbeat(Guid worker)
    {
        if (_trackers.TryGetValue(worker, out HeartbeatTracker? tracker))
        {
            tracker.RecordHeartbeat();
        }
    }

    public void RegisterWorker(Guid worker)
    {
        HeartbeatTracker tracker = new(worker, heartbeatTimeout);
        tracker.WorkerDead += OnWorkerDead;
        _trackers[worker] = tracker;
        tracker.Start();
        inner.RegisterWorker(worker);
    }

    public void UnregisterWorker(Guid worker)
    {
        if (_trackers.TryRemove(worker, out HeartbeatTracker? tracker))
        {
            tracker.WorkerDead -= OnWorkerDead;
            _ = tracker.DisposeAsync();
        }

        inner.UnregisterWorker(worker);
    }

    private void OnWorkerDead(Guid worker)
    {
        _ = inner.SendToWorkerAsync(worker, new UnregisteredBaseMessage(inner.Identifier, UnregisterReason.HeartbeatTimeout));
        UnregisterWorker(worker);
    }

    public Task SendToWorkerAsync(Guid workerIdentifier, BaseMessage baseMessage)
        => inner.SendToWorkerAsync(workerIdentifier, baseMessage);

    public Task BroadcastAsync(BaseMessage baseMessage)
        => inner.BroadcastAsync(baseMessage);

    public Task StartAsync()
        => inner.StartAsync();

    public async ValueTask DisposeAsync()
    {
        foreach (HeartbeatTracker tracker in _trackers.Values)
        {
            await tracker.DisposeAsync();
        }

        _trackers.Clear();
        await inner.DisposeAsync();
    }
}
