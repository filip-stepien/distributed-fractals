using System.Collections.Concurrent;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Core;

public abstract class MessageServerBase : IMessageServer
{
    private readonly ConcurrentDictionary<Guid, Guid> _clients = new();

    public IReadOnlyCollection<Guid> Clients => _clients.Values.ToList();

    public Guid Identifier { get; } = Guid.NewGuid();
    public event Action<Guid>? ClientRegistered;
    public event Action<Guid>? ClientUnregistered;
    public abstract event Action<BaseMessage>? MessageReceived;

    public void RegisterClient(Guid client)
    {
        _clients[client] = client;
        ClientRegistered?.Invoke(client);
    }

    public virtual void UnregisterClient(Guid client)
    {
        _clients.TryRemove(client, out _);
        ClientUnregistered?.Invoke(client);
    }

    public virtual string? GetClientAddress(Guid clientId) => null;

    public abstract Task StartAsync();
    public abstract Task SendToClientAsync(Guid clientIdentifier, BaseMessage baseMessage);
    public abstract Task BroadcastAsync(BaseMessage baseMessage);
    public abstract ValueTask DisposeAsync();
}
