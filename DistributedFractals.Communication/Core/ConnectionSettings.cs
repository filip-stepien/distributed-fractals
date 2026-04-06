namespace DistributedFractals.Server.Core;

public abstract record ConnectionSettings(string Address, int Port, TransportProtocol Protocol);
