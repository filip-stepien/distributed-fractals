using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Core;

public interface IMessageNode : IAsyncDisposable
{
    public Guid Identifier { get; }

    event Action<BaseMessage>? MessageReceived;

    Task StartAsync();
}