using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Handlers.Master;

public class MasterHeartbeatMessageHandler : IMessageHandler<HeartbeatMessage>
{
    public Task HandleAsync(HeartbeatMessage message)
    {
        Console.WriteLine($"[MASTER] Heartbeat from {message.Sender.DisplayName}");
        return Task.CompletedTask;
    }
}
