using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Core;

public interface IMessageServer : IMessageNode
{
    IReadOnlyCollection<ClientIdentifier> Clients { get; }

    event Action<ClientIdentifier>? ClientRegistered;
    event Action<ClientIdentifier>? ClientUnregistered;

    void RegisterClient(ClientIdentifier client);
    void UnregisterClient(ClientIdentifier client);

    /// <summary>
    /// Returns the network address (IP or "ip:port") this client connected from, if known.
    /// May return null for transports that don't track per-client endpoints.
    /// </summary>
    string? GetClientAddress(Guid clientId);

    Task SendToClientAsync(ClientIdentifier client, BaseMessage message);
    Task BroadcastAsync(BaseMessage message);
}
