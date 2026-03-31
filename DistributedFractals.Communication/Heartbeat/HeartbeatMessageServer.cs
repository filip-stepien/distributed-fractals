using System.Collections.Concurrent;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Heartbeat;

public sealed class HeartbeatMessageServer(IMessageServer inner, TimeSpan heartbeatTimeout) : IMessageServer
{
    private readonly ConcurrentDictionary<Guid, HeartbeatTracker> _trackers = new();

    public Guid Identifier => inner.Identifier;
    public IReadOnlyCollection<Guid> Clients => inner.Clients;

    public event Action<BaseMessage>? MessageReceived
    {
        add => inner.MessageReceived += value;
        remove => inner.MessageReceived -= value;
    }

    public event Action<Guid>? ClientRegistered
    {
        add => inner.ClientRegistered += value;
        remove => inner.ClientRegistered -= value;
    }

    public event Action<Guid>? ClientUnregistered
    {
        add => inner.ClientUnregistered += value;
        remove => inner.ClientUnregistered -= value;
    }

    public void RecordHeartbeat(Guid client)
    {
        if (_trackers.TryGetValue(client, out HeartbeatTracker? tracker))
        {
            tracker.RecordHeartbeat();
        }
    }

    public void RegisterClient(Guid client)
    {
        HeartbeatTracker tracker = new(client, heartbeatTimeout);
        tracker.ClientDead += OnClientDead;
        _trackers[client] = tracker;
        tracker.Start();
        inner.RegisterClient(client);
    }

    public void UnregisterClient(Guid client)
    {
        if (_trackers.TryRemove(client, out HeartbeatTracker? tracker))
        {
            tracker.ClientDead -= OnClientDead;
            _ = tracker.DisposeAsync();
        }

        inner.UnregisterClient(client);
    }

    private void OnClientDead(Guid client)
    {
        _ = inner.SendToClientAsync(client, new UnregisteredMessage(inner.Identifier, UnregisterReason.HeartbeatTimeout));
        UnregisterClient(client);
    }

    public Task SendToClientAsync(Guid clientIdentifier, BaseMessage baseMessage)
        => inner.SendToClientAsync(clientIdentifier, baseMessage);

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
