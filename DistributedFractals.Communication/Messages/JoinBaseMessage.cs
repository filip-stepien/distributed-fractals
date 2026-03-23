namespace DistributedFractals.Server.Messages;

public sealed record JoinBaseMessage(
    Guid Sender
) : BaseMessage(Sender);