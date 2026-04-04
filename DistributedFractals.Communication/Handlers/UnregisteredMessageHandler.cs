using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Handlers;

public class UnregisteredMessageHandler(Action? onUnregistered = null) : IMessageHandler<UnregisteredMessage>
{
    public Task HandleAsync(UnregisteredMessage message)
    {
        Console.WriteLine($"[WORKER] Unregistered by master. Reason: {message.Reason}");
        onUnregistered?.Invoke();
        return Task.CompletedTask;
    }
}
