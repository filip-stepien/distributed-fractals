namespace DistributedFractals.Server.Messages;

public enum UnregisterReason
{
    HeartbeatTimeout
}

public sealed record UnregisteredBaseMessage(
    Guid Sender,
    UnregisterReason Reason
) : BaseMessage(Sender);
