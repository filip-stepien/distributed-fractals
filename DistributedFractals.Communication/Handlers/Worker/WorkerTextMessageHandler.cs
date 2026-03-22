using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Handlers.Worker;

public class WorkerTextMessageHandler : IMessageHandler<TextMessage>
{
    public Task HandleAsync(TextMessage message)
    {
        Console.WriteLine($"[WORKER] Text from {message.Sender.DisplayName}: {message.Text}");
        return Task.CompletedTask;
    }
}
