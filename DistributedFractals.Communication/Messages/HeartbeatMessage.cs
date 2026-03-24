namespace DistributedFractals.Server.Messages;

public sealed record HeartbeatMessage(
    Guid Sender
) : BaseMessage(Sender);