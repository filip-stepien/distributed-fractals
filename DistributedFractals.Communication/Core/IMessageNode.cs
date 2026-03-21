namespace DistributedFractals.Server.Core;

public interface IMessageNode : IAsyncDisposable
{
    public MessageNodeIdentifier Identifier { get; }
    
    Task StartAsync();
}