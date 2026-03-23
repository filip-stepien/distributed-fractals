using DistributedFractals.Server.Heartbeat;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Handlers.Master;

public class MasterHeartbeatMessageHandler(HeartbeatMasterNode master) : IMessageHandler<HeartbeatBaseMessage>
{
    public Task HandleAsync(HeartbeatBaseMessage baseMessage)
    {
        master.RecordHeartbeat(baseMessage.Sender);
        return Task.CompletedTask;
    }
}
