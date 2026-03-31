using DistributedFractals.Server.Heartbeat;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Handlers;

public class HeartbeatMessageHandler(HeartbeatMessageServer master) : IMessageHandler<HeartbeatMessage>
{
    public Task HandleAsync(HeartbeatMessage message)
    {
        master.RecordHeartbeat(message.Sender);
        return Task.CompletedTask;
    }
}
