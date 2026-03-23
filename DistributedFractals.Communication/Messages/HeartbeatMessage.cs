namespace DistributedFractals.Server.Messages;

public sealed record HeartbeatBaseMessage(
    Guid Sender
) : BaseMessage(Sender);