namespace DistributedFractals.Server.Messages;

public sealed record RenderBatchMessage(
    Guid Sender,
    int BatchId,
    IReadOnlyList<RenderFrameMessage> Frames
) : BaseMessage(Sender);
