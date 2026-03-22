using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Handlers.Worker;

public class WorkerHeartbeatMessageHandler : IMessageHandler<HeartbeatMessage>
{
    public Task HandleAsync(HeartbeatMessage message)
    {
        Console.WriteLine($"[WORKER] Heartbeat from {message.Sender.DisplayName}");
        return Task.CompletedTask;
    }
}
