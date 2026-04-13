using DistributedFractals.Fractal.Colorizers;
using DistributedFractals.Fractal.Generators;
using DistributedFractals.Fractal.Zoom;
using DistributedFractals.Orchestration.Selectors;
using DistributedFractals.Video;

namespace DistributedFractals.Sessions;

public sealed record RenderSettings(
    IReadOnlyList<ZoomKeyframe> Keyframes,
    IFractalGeneratorOptions Options,
    FractalColorizerType Colorizer,
    int TotalFrames,
    int Fps,
    ZoomInterpolationType Interpolation,
    int FramesPerClient,
    ClientSelectorType ClientSelector,
    VideoFormat OutputFormat,
    string OutputPath
);
