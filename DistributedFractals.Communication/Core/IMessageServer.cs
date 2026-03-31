using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Core;

public interface IMessageServer : IMessageNode
{
    IReadOnlyCollection<Guid> Clients { get; }

    event Action<Guid>? ClientRegistered;
    event Action<Guid>? ClientUnregistered;

    void RegisterClient(Guid client);
    void UnregisterClient(Guid client);

    Task SendToClientAsync(Guid clientIdentifier, BaseMessage baseMessage);
    Task BroadcastAsync(BaseMessage baseMessage);
}
