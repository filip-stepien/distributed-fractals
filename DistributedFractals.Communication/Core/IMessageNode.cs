namespace DistributedFractals.Server.Core;

public interface IMessageNode : IAsyncDisposable
{
    public MessageNodeIdentifier Identifier { get; }

    event Action<Message>? MessageReceived;

    Task ConnectAsync();
    Task SendAsync(Message message);
}