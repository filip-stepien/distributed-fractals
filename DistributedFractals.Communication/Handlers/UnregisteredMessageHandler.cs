using DistributedFractals.Logging;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Handlers;

public class UnregisteredMessageHandler(Action? onUnregistered = null) : IMessageHandler<UnregisteredMessage>
{
    public Task HandleAsync(UnregisteredMessage message)
    {
        Logger.Log($"Unregistered by server. Reason: {message.Reason}");
        onUnregistered?.Invoke();
        return Task.CompletedTask;
    }
}
