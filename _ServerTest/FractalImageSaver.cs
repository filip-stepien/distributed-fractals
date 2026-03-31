using DistributedFractals.Core.Core;
using SkiaSharp;

namespace ServerTest;

public static class FractalImageSaver
{
    public static string Save(FractalResult result, string? fileName = null)
    {
        using SKBitmap bitmap = new((int)result.Width, (int)result.Height);

        foreach (FractalPoint point in result.FractalPoints)
        {
            int x = (int)point.Coordinates.X;
            int y = (int)point.Coordinates.Y;
            byte r = (byte)(point.Color.X * 255);
            byte g = (byte)(point.Color.Y * 255);
            byte b = (byte)(point.Color.Z * 255);
            bitmap.SetPixel(x, y, new SKColor(r, g, b));
        }

        fileName ??= $"fractal_{DateTime.Now:yyyyMMdd_HHmmss}";
        string path = Path.Combine(Path.GetTempPath(), $"{fileName}.png");
        using SKFileWStream stream = new(path);
        bitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
        return path;
    }
}
