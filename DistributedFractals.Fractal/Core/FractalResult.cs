namespace DistributedFractals.Fractal.Core;

// Pixels: row-major RGB, 3 bytes per pixel, index = (y * Width + x) * 3
public record FractalResult(ulong Width, ulong Height, byte[] Pixels);
