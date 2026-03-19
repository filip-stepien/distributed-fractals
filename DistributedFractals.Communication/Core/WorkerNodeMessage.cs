namespace DistributedFractals.Server.Core;

public sealed record WorkerNodeMessage(
    MessageNodeIdentifier Sender,
    string Content
);