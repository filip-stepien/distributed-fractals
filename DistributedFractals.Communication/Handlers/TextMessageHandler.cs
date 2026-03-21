using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Handlers;

public class TextMessageHandler : IMessageHandler<TextMessage>
{
    public Task HandleAsync(TextMessage message)
    {
        Console.WriteLine($"[TEXT] From {message.Sender.DisplayName}: {message.Text}");
        return Task.CompletedTask;
    }
}
