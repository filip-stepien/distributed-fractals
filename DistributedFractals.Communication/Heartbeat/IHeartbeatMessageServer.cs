using DistributedFractals.Server.Core;

namespace DistributedFractals.Server.Heartbeat;

public interface IHeartbeatMessageServer : IMessageServer
{
    void RecordHeartbeat(Guid clientId);
}
