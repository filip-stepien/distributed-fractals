using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Handlers.Worker;

public class UnregisteredMessageHandler : IMessageHandler<UnregisteredMessage>
{
    public Task HandleAsync(UnregisteredMessage message)
    {
        Console.WriteLine($"[WORKER] Unregistered by master. Reason: {message.Reason}");
        return Task.CompletedTask;
    }
}
