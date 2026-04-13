using System.Collections.Concurrent;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Core;

public abstract class MessageServerBase : IMessageServer
{
    private readonly ConcurrentDictionary<Guid, ClientIdentifier> _clients = new();

    public IReadOnlyCollection<ClientIdentifier> Clients => _clients.Values.ToList();

    public Guid Identifier { get; } = Guid.NewGuid();
    public event Action<ClientIdentifier>? ClientRegistered;
    public event Action<ClientIdentifier>? ClientUnregistered;
    public abstract event Action<BaseMessage>? MessageReceived;

    public void RegisterClient(ClientIdentifier client)
    {
        _clients[client.Id] = client;
        ClientRegistered?.Invoke(client);
    }

    public virtual void UnregisterClient(ClientIdentifier client)
    {
        _clients.TryRemove(client.Id, out _);
        ClientUnregistered?.Invoke(client);
    }

    public virtual string? GetClientAddress(Guid clientId) => null;

    public abstract Task StartAsync();
    public abstract Task SendToClientAsync(ClientIdentifier client, BaseMessage message);
    public abstract Task BroadcastAsync(BaseMessage message);
    public abstract ValueTask DisposeAsync();
}
