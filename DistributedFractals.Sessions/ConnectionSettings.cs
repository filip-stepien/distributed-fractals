namespace DistributedFractals.Sessions;

public sealed record ConnectionSettings(
    string Address,
    int Port,
    TransportProtocol Protocol,
    TimeSpan ClientTimeout,
    TimeSpan HeartbeatInterval
);
