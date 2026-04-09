using System.Collections.Generic;
using DistributedFractals.Core.Core;
using DistributedFractals.Core.Generators.Mandelbrot;
using DistributedFractals.Core.Zoom;

namespace DistributedFractals.Gui.Networking;

/// <summary>Everything the GUI needs to kick off a distributed render job.</summary>
public sealed record RenderJobConfig(
    MandelbrotOptions BaseOptions,
    IReadOnlyList<ZoomKeyframe> Keyframes,
    int TotalFrames,
    int FrameRate,
    FractalColorizerType Colorizer,
    string OutputPath
);
