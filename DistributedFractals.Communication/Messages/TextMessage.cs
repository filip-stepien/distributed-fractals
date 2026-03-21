using DistributedFractals.Server.Core;

namespace DistributedFractals.Server.Messages;

public sealed record TextMessage(
    MessageNodeIdentifier Sender,
    string Text
) : Message(Sender);