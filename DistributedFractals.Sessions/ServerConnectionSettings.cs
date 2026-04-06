using DistributedFractals.Server.Core;

namespace DistributedFractals.Sessions;

public sealed record ServerConnectionSettings(
    string Address,
    int Port,
    TransportProtocol Protocol,
    TimeSpan ClientTimeout
) : ConnectionSettings(Address, Port, Protocol);
