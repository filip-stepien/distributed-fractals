namespace DistributedFractals.Server.Messages;

public enum UnregisterReason
{
    HeartbeatTimeout
}

public sealed record UnregisteredMessage(
    Guid Sender,
    UnregisterReason Reason
) : BaseMessage(Sender);
