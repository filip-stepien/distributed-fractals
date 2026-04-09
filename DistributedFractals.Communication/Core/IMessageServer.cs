using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Core;

public interface IMessageServer : IMessageNode
{
    IReadOnlyCollection<Guid> Clients { get; }

    event Action<Guid>? ClientRegistered;
    event Action<Guid>? ClientUnregistered;

    void RegisterClient(Guid client);
    void UnregisterClient(Guid client);

    /// <summary>
    /// Returns the network address (IP or "ip:port") this client connected from, if known.
    /// May return null for transports that don't track per-client endpoints.
    /// </summary>
    string? GetClientAddress(Guid clientId);

    Task SendToClientAsync(Guid clientIdentifier, BaseMessage baseMessage);
    Task BroadcastAsync(BaseMessage baseMessage);
}
