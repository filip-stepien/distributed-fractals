using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Handlers.Worker;

public class WorkerUnregisteredMessageHandler : IMessageHandler<UnregisteredBaseMessage>
{
    public Task HandleAsync(UnregisteredBaseMessage message)
    {
        Console.WriteLine($"[WORKER] Unregistered by master. Reason: {message.Reason}");
        return Task.CompletedTask;
    }
}
