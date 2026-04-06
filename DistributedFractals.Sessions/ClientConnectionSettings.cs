using DistributedFractals.Server.Core;

namespace DistributedFractals.Sessions;

public sealed record ClientConnectionSettings(
    string Address,
    int Port,
    TransportProtocol Protocol,
    TimeSpan HeartbeatInterval
) : ConnectionSettings(Address, Port, Protocol);
