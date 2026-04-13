using DistributedFractals.Video.Gif;

namespace DistributedFractals.Video;

public static class VideoWriterFactory
{
    public static IVideoWriter Create(string outputPath, int fps, VideoFormat format)
    {
        return format switch
        {
            VideoFormat.Gif => new GifVideoWriter(outputPath, fps, repeat: true),
            _ => throw new NotSupportedException($"Unsupported video format: {format}")
        };
    }
}
