using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Handlers;

public class HeartbeatMessageHandler : IMessageHandler<HeartbeatMessage>
{
    public Task HandleAsync(HeartbeatMessage message)
    {
        Console.WriteLine($"[HEARTBEAT] From {message.Sender.DisplayName}");
        return Task.CompletedTask;
    }
}
