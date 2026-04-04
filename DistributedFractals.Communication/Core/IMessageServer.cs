using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Core;

public interface IMessageServer : IMessageNode
{
    IReadOnlyCollection<ClientIdentifier> Clients { get; }

    event Action<ClientIdentifier>? ClientRegistered;
    event Action<ClientIdentifier>? ClientUnregistered;

    void RegisterClient(ClientIdentifier client);
    void UnregisterClient(ClientIdentifier client);

    Task SendToClientAsync(ClientIdentifier client, BaseMessage message);
    Task BroadcastAsync(BaseMessage message);
}
