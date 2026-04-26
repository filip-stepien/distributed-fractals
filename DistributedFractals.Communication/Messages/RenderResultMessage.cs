using DistributedFractals.Fractal.Core;

namespace DistributedFractals.Server.Messages;

public sealed record RenderResultMessage(
    Guid Sender,
    int FrameIndex,
    FractalResult Result,
    TimeSpan RenderDuration
) : BaseMessage(Sender);
