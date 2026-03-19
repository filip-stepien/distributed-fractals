namespace DistributedFractals.Server.Core;

public sealed record Message(
    MessageNodeIdentifier Sender,
    MessageNodeIdentifier Receiver,
    string Content
);