using System.Numerics;
using DistributedFractals.Fractal.Colorizers;
using DistributedFractals.Fractal.Core;
using DistributedFractals.Fractal.Mandelbrot;
using DistributedFractals.Fractal.Zoom;

namespace DistributedFractals.Fractal.Generators.Mandelbrot;

public class MandelbrotGenerator : FractalGeneratorBase<MandelbrotOptions>
{
    protected override FractalResult Generate(MandelbrotOptions options, FrameBounds bounds, IFractalColorizer colorizer)
    {
        // read generator options
        ulong width = options.Width;
        ulong height = options.Height;
        ulong maxIterations = options.MaxIterations;
        double minRe = bounds.MinRe;
        double maxRe = bounds.MaxRe;
        double minIm = bounds.MinIm;
        double maxIm = bounds.MaxIm;

        FractalPoint[] resultingPoints = new FractalPoint[width * height];

        Parallel.For(0, (long)width, pixelX =>
        {
            for (ulong pixelY = 0; pixelY < height; pixelY++)
            {
                double normalizedPixelX = pixelX / (double)(width - 1);
                double normalizedPixelY = pixelY / (double)(height - 1);
                double cReal = minRe + normalizedPixelX * (maxRe - minRe);
                double cImaginary = minIm + normalizedPixelY * (maxIm - minIm);

                Complex c = new(cReal, cImaginary);
                Complex z = Complex.Zero;
                ulong iteration = 0;

                while (z.Real * z.Real + z.Imaginary * z.Imaginary <= 4.0 && iteration < maxIterations)
                {
                    z = z * z + c;
                    iteration++;
                }

                resultingPoints[pixelX * (long)height + (long)pixelY] = new FractalPoint(
                    Coordinates: new Vector2(pixelX, pixelY),
                    Color: colorizer.GetColor(iteration, maxIterations)
                );
            }
        });

        return new FractalResult(width, height, resultingPoints);
    }
}
