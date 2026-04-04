using DistributedFractals.Core.Core;
using DistributedFractals.Core.Zoom;

namespace DistributedFractals.Server.Messages;

public sealed record RenderFrameMessage(
    Guid Sender,
    int FrameIndex,
    FractalGeneratorType GeneratorType,
    FractalColorizerType ColorizerType,
    IFractalGeneratorOptions Options,
    FrameBounds Bounds
) : BaseMessage(Sender);
