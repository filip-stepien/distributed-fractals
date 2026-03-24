namespace DistributedFractals.Server.Messages;

public sealed record JoinMessage(
    Guid Sender
) : BaseMessage(Sender);