namespace DistributedFractals.Server.Messages;

public sealed record JoinMessage(
    Guid Sender,
    string DisplayName = ""
) : BaseMessage(Sender);