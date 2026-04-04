using System.Collections.Concurrent;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Heartbeat;

public sealed class HeartbeatMessageServer(IMessageServer inner, TimeSpan heartbeatTimeout) : IHeartbeatMessageServer
{
    private readonly ConcurrentDictionary<Guid, HeartbeatTracker> _trackers = new();
    private readonly ConcurrentDictionary<Guid, ClientIdentifier> _identifiers = new();

    public Guid Identifier => inner.Identifier;
    public IReadOnlyCollection<ClientIdentifier> Clients => inner.Clients;

    public event Action<BaseMessage>? MessageReceived
    {
        add => inner.MessageReceived += value;
        remove => inner.MessageReceived -= value;
    }

    public event Action<ClientIdentifier>? ClientRegistered
    {
        add => inner.ClientRegistered += value;
        remove => inner.ClientRegistered -= value;
    }

    public event Action<ClientIdentifier>? ClientUnregistered
    {
        add => inner.ClientUnregistered += value;
        remove => inner.ClientUnregistered -= value;
    }

    public void RecordHeartbeat(Guid clientId)
    {
        if (_trackers.TryGetValue(clientId, out HeartbeatTracker? tracker))
        {
            tracker.RecordHeartbeat();
        }
    }

    public void RegisterClient(ClientIdentifier client)
    {
        HeartbeatTracker tracker = new(client.Id, heartbeatTimeout);
        tracker.ClientDead += OnClientDead;
        _trackers[client.Id] = tracker;
        _identifiers[client.Id] = client;
        tracker.Start();
        inner.RegisterClient(client);
    }

    public void UnregisterClient(ClientIdentifier client)
    {
        if (_trackers.TryRemove(client.Id, out HeartbeatTracker? tracker))
        {
            tracker.ClientDead -= OnClientDead;
            _ = tracker.DisposeAsync();
        }

        _identifiers.TryRemove(client.Id, out _);
        inner.UnregisterClient(client);
    }

    private void OnClientDead(Guid clientId)
    {
        if (!_identifiers.TryGetValue(clientId, out ClientIdentifier? client))
        {
            return;
        }

        _ = inner.SendToClientAsync(client, new UnregisteredMessage(inner.Identifier, UnregisterReason.HeartbeatTimeout));
        UnregisterClient(client);
    }

    public Task SendToClientAsync(ClientIdentifier client, BaseMessage message)
        => inner.SendToClientAsync(client, message);

    public Task BroadcastAsync(BaseMessage message)
        => inner.BroadcastAsync(message);

    public Task StartAsync()
        => inner.StartAsync();

    public async ValueTask DisposeAsync()
    {
        foreach (HeartbeatTracker tracker in _trackers.Values)
        {
            await tracker.DisposeAsync();
        }

        _trackers.Clear();
        _identifiers.Clear();
        await inner.DisposeAsync();
    }
}
