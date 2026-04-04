using DistributedFractals.Core.Core;
using DistributedFractals.Core.Zoom;
using DistributedFractals.Orchestration.Selectors;

namespace DistributedFractals.Sessions;

public sealed record RenderSettings(
    IReadOnlyList<ZoomKeyframe> Keyframes,
    FractalGeneratorType GeneratorType,
    IFractalGeneratorOptions Options,
    FractalColorizerType Colorizer,
    int TotalFrames,
    int Fps,
    IZoomInterpolation Interpolation,
    int FramesPerClient,
    IClientSelector ClientSelector,
    VideoFormat OutputFormat,
    string OutputPath
);
