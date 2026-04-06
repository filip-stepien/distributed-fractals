namespace DistributedFractals.Fractal.Core;

public record FractalResult(ulong Width, ulong Height, ICollection<FractalPoint> FractalPoints);
