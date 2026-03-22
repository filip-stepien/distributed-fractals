using DistributedFractals.Server.Core;

namespace DistributedFractals.Server.Messages;

public sealed record JoinMessage(
    MessageNodeIdentifier Sender
) : Message(Sender);