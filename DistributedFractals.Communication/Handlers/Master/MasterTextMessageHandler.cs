using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Handlers.Master;

public class MasterTextMessageHandler : IMessageHandler<TextMessage>
{
    public Task HandleAsync(TextMessage message)
    {
        Console.WriteLine($"[MASTER] Text from {message.Sender.DisplayName}: {message.Text}");
        return Task.CompletedTask;
    }
}
