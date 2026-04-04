using DistributedFractals.Core.Zoom;

namespace DistributedFractals.Core.Core;

public interface IFractalGeneratorOptions
{
    ulong Width { get; }
    ulong Height { get; }
    FrameBounds DefaultBounds { get; }
}