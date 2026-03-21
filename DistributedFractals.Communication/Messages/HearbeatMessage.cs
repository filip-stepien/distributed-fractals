using DistributedFractals.Server.Core;

namespace DistributedFractals.Server.Messages;

public sealed record HeartbeatMessage(
    MessageNodeIdentifier Sender
) : Message(Sender);