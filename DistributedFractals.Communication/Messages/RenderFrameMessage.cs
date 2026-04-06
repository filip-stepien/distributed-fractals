using DistributedFractals.Fractal.Colorizers;
using DistributedFractals.Fractal.Core;
using DistributedFractals.Fractal.Generators;
using DistributedFractals.Fractal.Zoom;

namespace DistributedFractals.Server.Messages;

public sealed record RenderFrameMessage(
    Guid Sender,
    int FrameIndex,
    FractalColorizerType ColorizerType,
    IFractalGeneratorOptions Options,
    FrameBounds Bounds
) : BaseMessage(Sender);
