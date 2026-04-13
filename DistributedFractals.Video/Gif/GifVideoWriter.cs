using DistributedFractals.Fractal.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;

namespace DistributedFractals.Video.Gif;

public sealed class GifVideoWriter(string outputPath, int frameRate, bool repeat = true) : IVideoWriter
{
    private readonly int _frameDelayHundredths = 100 / frameRate;
    private Image<Rgba32>? _gif;

    public Task WriteFrameAsync(FractalResult frame)
    {
        if (_gif is null)
        {
            _gif = new Image<Rgba32>((int)frame.Width, (int)frame.Height);
            _gif.Metadata.GetGifMetadata().RepeatCount = repeat ? (ushort)0 : (ushort)1;
            FillFrame(_gif.Frames.RootFrame, frame);
            SetDelay(_gif.Frames.RootFrame);
        }
        else
        {
            var imageFrame = _gif.Frames.AddFrame(_gif.Frames.RootFrame);
            FillFrame(imageFrame, frame);
            SetDelay(imageFrame);
        }

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_gif is not null)
        {
            await _gif.SaveAsGifAsync(outputPath);
            _gif.Dispose();
        }
    }

    private static void FillFrame(ImageFrame<Rgba32> imageFrame, FractalResult frame)
    {
        foreach (FractalPoint point in frame.FractalPoints)
        {
            imageFrame[(int)point.Coordinates.X, (int)point.Coordinates.Y] = new Rgba32(
                (byte)(point.Color.X * 255),
                (byte)(point.Color.Y * 255),
                (byte)(point.Color.Z * 255));
        }
    }

    private void SetDelay(ImageFrame<Rgba32> imageFrame)
    {
        imageFrame.Metadata.GetGifMetadata().FrameDelay = _frameDelayHundredths;
    }
}
