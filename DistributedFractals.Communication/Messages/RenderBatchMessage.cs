namespace DistributedFractals.Server.Messages;

public sealed record RenderBatchMessage(
    Guid Sender,
    Guid RenderJobId,
    int BatchId,
    IReadOnlyList<RenderFrameMessage> Frames
) : BaseMessage(Sender);
