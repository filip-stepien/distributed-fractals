using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Core;

public interface IMessageNode : IAsyncDisposable
{
    public MessageNodeIdentifier Identifier { get; }
    
    event Action<Message>? MessageReceived;
    
    Task StartAsync();
}