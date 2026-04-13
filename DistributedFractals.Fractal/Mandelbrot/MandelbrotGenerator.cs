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

        // Stores all points generated for the final fractal image.
        List<FractalPoint> resultingPoints = [];

        // Iterate over every pixel in the viewport.
        for (uint pixelX = 0; pixelX < width; pixelX++)
        {
            for (uint pixelY = 0; pixelY < height; pixelY++)
            {
                // Map pixel coordinates to a point on the complex plane.
                // First normalize pixel coordinates to the 0..1 range,
                // then scale them to the complex-plane bounds.
                double normalizedPixelX = pixelX / (double)(width - 1);
                double normalizedPixelY = pixelY / (double)(height - 1);
                double cReal = minRe + normalizedPixelX * (maxRe - minRe);
                double cImaginary = minIm + normalizedPixelY * (maxIm - minIm);

                // Create the constant c for the current pixel.
                Complex c = new(cReal, cImaginary);

                // Start Mandelbrot iteration from z = 0.
                Complex z = Complex.Zero;

                // Counts how many iterations the point remains bounded.
                ulong iteration = 0;

                // Apply the Mandelbrot recurrence:
                // z = z^2 + c
                //
                // As long as |z| <= 2, the point has not escaped.
                // Instead of calculating |z| directly, we check |z|^2 <= 4
                // to avoid the square root operation.
                while (z.Real * z.Real + z.Imaginary * z.Imaginary <= 4.0 && iteration < maxIterations)
                {
                    z = z * z + c;
                    iteration++;
                }

                // Add the pixel with a color based on the number of iterations.
                resultingPoints.Add(new FractalPoint(
                    Coordinates: new Vector2(pixelX, pixelY),
                    Color: colorizer.GetColor(iteration, maxIterations)
                ));
            }
        }

        return new FractalResult(width, height, resultingPoints);
    }
}
