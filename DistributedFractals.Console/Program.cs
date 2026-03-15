using System.Drawing;
using DistributedFractals.Core.Colorizers;
using DistributedFractals.Core.Core;
using DistributedFractals.Core.Generators.Mandelbrot;

var options = new MandelbrotOptions(
    Width: 800,
    Height: 600,
    MaxIterations: 1000
);

var generator = new MandelbrotGenerator();
var colorizer = new BlackAndWhiteColorizer();

FractalResult result = generator.Generate(options, colorizer);

// DEBUG: save to bitmap
var bitmap = new Bitmap((int)result.Width, (int)result.Height);
foreach (FractalPoint point in result.FractalPoints)
{
    int r = (int)(point.Color.X * 255);
    int g = (int)(point.Color.Y * 255);
    int b = (int)(point.Color.Z * 255);
    bitmap.SetPixel((int)point.Coordinates.X, (int)point.Coordinates.Y, Color.FromArgb(r, g, b));
}

string outputPath = "mandelbrot.png";
bitmap.Save(outputPath);
Console.WriteLine($"Saved to {Path.GetFullPath(outputPath)}");