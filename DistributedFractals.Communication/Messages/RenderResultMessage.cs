using DistributedFractals.Core.Core;

namespace DistributedFractals.Server.Messages;

public sealed record RenderResultMessage(
    Guid Sender,
    int FrameIndex,
    FractalResult Result
) : BaseMessage(Sender);
