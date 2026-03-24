using DistributedFractals.Server.Heartbeat;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Handlers.Master;

public class HeartbeatMessageHandler(HeartbeatMessageMasterNode master) : IMessageHandler<HeartbeatMessage>
{
    public Task HandleAsync(HeartbeatMessage message)
    {
        master.RecordHeartbeat(message.Sender);
        return Task.CompletedTask;
    }
}
