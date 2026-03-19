namespace DistributedFractals.Server.Core;

public interface IMessageNode
{
    public MessageNodeIdentifier Identifier { get; }
    
    event Action<Message>? MessageReceived;

    Task StartAsync();
    Task StopAsync();
    Task SendAsync(Message message);
}