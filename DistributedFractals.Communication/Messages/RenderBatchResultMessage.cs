namespace DistributedFractals.Server.Messages;

public sealed record RenderFrameResult(
    int FrameIndex,
    DistributedFractals.Fractal.Core.FractalResult Result,
    TimeSpan RenderDuration
);

public sealed record RenderBatchResultMessage(
    Guid Sender,
    int BatchId,
    IReadOnlyList<RenderFrameResult> Results,
    TimeSpan RenderDuration
) : BaseMessage(Sender);
