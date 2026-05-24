using DistributedFractals.Fractal.Colorizers;
using DistributedFractals.Fractal.Core;
using DistributedFractals.Fractal.Generators;
using DistributedFractals.Fractal.Mandelbrot;
using DistributedFractals.Fractal.Zoom;
using DistributedFractals.Server.Messages;
using System.Runtime.InteropServices;

namespace DistributedFractals.Server.Serializers;

public sealed class BinaryMessageSerializer : ISerializer
{
    private enum MessageKind : byte
    {
        Join = 1,
        Heartbeat = 2,
        RenderFrame = 3,
        RenderResult = 4,
        RenderBatch = 5,
        RenderBatchResult = 6,
        Unregistered = 7
    }

    public ReadOnlyMemory<byte> Serialize<T>(T data)
    {
        if (data is not BaseMessage message)
        {
            throw new NotSupportedException($"Binary serializer supports {nameof(BaseMessage)} only.");
        }

        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        switch (message)
        {
            case JoinMessage join:
                writer.Write((byte)MessageKind.Join);
                WriteBase(writer, join);
                writer.Write(join.DisplayName);
                break;

            case HeartbeatMessage heartbeat:
                writer.Write((byte)MessageKind.Heartbeat);
                WriteBase(writer, heartbeat);
                break;

            case RenderFrameMessage renderFrame:
                writer.Write((byte)MessageKind.RenderFrame);
                WriteRenderFrame(writer, renderFrame);
                break;

            case RenderResultMessage renderResult:
                writer.Write((byte)MessageKind.RenderResult);
                WriteBase(writer, renderResult);
                WriteGuid(writer, renderResult.RenderJobId);
                writer.Write(renderResult.FrameIndex);
                WriteFractalResult(writer, renderResult.Result);
                writer.Write(renderResult.RenderDuration.Ticks);
                break;

            case RenderBatchMessage renderBatch:
                writer.Write((byte)MessageKind.RenderBatch);
                WriteBase(writer, renderBatch);
                WriteGuid(writer, renderBatch.RenderJobId);
                writer.Write(renderBatch.BatchId);
                writer.Write(renderBatch.Frames.Count);
                foreach (RenderFrameMessage frame in renderBatch.Frames)
                {
                    WriteRenderFrame(writer, frame);
                }
                break;

            case RenderBatchResultMessage batchResult:
                writer.Write((byte)MessageKind.RenderBatchResult);
                WriteBase(writer, batchResult);
                WriteGuid(writer, batchResult.RenderJobId);
                writer.Write(batchResult.BatchId);
                writer.Write(batchResult.Results.Count);
                foreach (RenderFrameResult result in batchResult.Results)
                {
                    writer.Write(result.FrameIndex);
                    WriteFractalResult(writer, result.Result);
                    writer.Write(result.RenderDuration.Ticks);
                }
                writer.Write(batchResult.RenderDuration.Ticks);
                break;

            case UnregisteredMessage unregistered:
                writer.Write((byte)MessageKind.Unregistered);
                WriteBase(writer, unregistered);
                writer.Write((int)unregistered.Reason);
                break;

            default:
                throw new NotSupportedException($"Unsupported message type: {message.GetType().Name}");
        }

        return stream.ToArray();
    }

    public T Deserialize<T>(ReadOnlyMemory<byte> bytes)
    {
        using MemoryStream stream = CreateReadableStream(bytes);
        using BinaryReader reader = new(stream);

        MessageKind kind = (MessageKind)reader.ReadByte();
        BaseMessage message = kind switch
        {
            MessageKind.Join => new JoinMessage(ReadGuid(reader), reader.ReadString()),
            MessageKind.Heartbeat => new HeartbeatMessage(ReadGuid(reader)),
            MessageKind.RenderFrame => ReadRenderFrame(reader),
            MessageKind.RenderResult => ReadRenderResult(reader),
            MessageKind.RenderBatch => ReadRenderBatch(reader),
            MessageKind.RenderBatchResult => ReadRenderBatchResult(reader),
            MessageKind.Unregistered => new UnregisteredMessage(ReadGuid(reader), (UnregisterReason)reader.ReadInt32()),
            _ => throw new NotSupportedException($"Unsupported message kind: {kind}")
        };

        if (message is not T typed)
        {
            throw new InvalidOperationException($"Failed to deserialize data to type {typeof(T).Name}.");
        }

        return typed;
    }

    private static void WriteBase(BinaryWriter writer, BaseMessage message)
    {
        WriteGuid(writer, message.Sender);
    }

    private static void WriteRenderFrame(BinaryWriter writer, RenderFrameMessage message)
    {
        WriteBase(writer, message);
        WriteGuid(writer, message.RenderJobId);
        writer.Write(message.FrameIndex);
        writer.Write((int)message.ColorizerType);
        WriteOptions(writer, message.Options);
        WriteBounds(writer, message.Bounds);
    }

    private static RenderFrameMessage ReadRenderFrame(BinaryReader reader)
    {
        Guid sender = ReadGuid(reader);
        Guid renderJobId = ReadGuid(reader);
        int frameIndex = reader.ReadInt32();
        FractalColorizerType colorizerType = (FractalColorizerType)reader.ReadInt32();
        IFractalGeneratorOptions options = ReadOptions(reader);
        FrameBounds bounds = ReadBounds(reader);

        return new RenderFrameMessage(sender, renderJobId, frameIndex, colorizerType, options, bounds);
    }

    private static RenderResultMessage ReadRenderResult(BinaryReader reader)
    {
        Guid sender = ReadGuid(reader);
        Guid renderJobId = ReadGuid(reader);
        int frameIndex = reader.ReadInt32();
        FractalResult result = ReadFractalResult(reader);
        TimeSpan duration = TimeSpan.FromTicks(reader.ReadInt64());

        return new RenderResultMessage(sender, renderJobId, frameIndex, result, duration);
    }

    private static RenderBatchMessage ReadRenderBatch(BinaryReader reader)
    {
        Guid sender = ReadGuid(reader);
        Guid renderJobId = ReadGuid(reader);
        int batchId = reader.ReadInt32();
        int frameCount = reader.ReadInt32();
        List<RenderFrameMessage> frames = new(frameCount);

        for (int i = 0; i < frameCount; i++)
        {
            frames.Add(ReadRenderFrame(reader));
        }

        return new RenderBatchMessage(sender, renderJobId, batchId, frames);
    }

    private static RenderBatchResultMessage ReadRenderBatchResult(BinaryReader reader)
    {
        Guid sender = ReadGuid(reader);
        Guid renderJobId = ReadGuid(reader);
        int batchId = reader.ReadInt32();
        int resultCount = reader.ReadInt32();
        List<RenderFrameResult> results = new(resultCount);

        for (int i = 0; i < resultCount; i++)
        {
            int frameIndex = reader.ReadInt32();
            FractalResult result = ReadFractalResult(reader);
            TimeSpan duration = TimeSpan.FromTicks(reader.ReadInt64());
            results.Add(new RenderFrameResult(frameIndex, result, duration));
        }

        TimeSpan batchDuration = TimeSpan.FromTicks(reader.ReadInt64());
        return new RenderBatchResultMessage(sender, renderJobId, batchId, results, batchDuration);
    }

    private static void WriteOptions(BinaryWriter writer, IFractalGeneratorOptions options)
    {
        writer.Write((int)options.GeneratorType);

        switch (options)
        {
            case MandelbrotOptions mandelbrot:
                writer.Write(mandelbrot.Width);
                writer.Write(mandelbrot.Height);
                writer.Write(mandelbrot.MaxIterations);
                break;

            default:
                throw new NotSupportedException($"Unsupported fractal options type: {options.GetType().Name}");
        }
    }

    private static IFractalGeneratorOptions ReadOptions(BinaryReader reader)
    {
        FractalGeneratorType generatorType = (FractalGeneratorType)reader.ReadInt32();

        return generatorType switch
        {
            FractalGeneratorType.Mandelbrot => new MandelbrotOptions(
                Width: reader.ReadUInt64(),
                Height: reader.ReadUInt64(),
                MaxIterations: reader.ReadUInt64()),
            _ => throw new NotSupportedException($"Unsupported generator type: {generatorType}")
        };
    }

    private static void WriteBounds(BinaryWriter writer, FrameBounds bounds)
    {
        writer.Write(bounds.MinRe);
        writer.Write(bounds.MaxRe);
        writer.Write(bounds.MinIm);
        writer.Write(bounds.MaxIm);
    }

    private static FrameBounds ReadBounds(BinaryReader reader)
    {
        return new FrameBounds(
            MinRe: reader.ReadDouble(),
            MaxRe: reader.ReadDouble(),
            MinIm: reader.ReadDouble(),
            MaxIm: reader.ReadDouble());
    }

    private static void WriteFractalResult(BinaryWriter writer, FractalResult result)
    {
        writer.Write(result.Width);
        writer.Write(result.Height);
        writer.Write(result.Pixels.Length);
        writer.Write(result.Pixels);
    }

    private static FractalResult ReadFractalResult(BinaryReader reader)
    {
        ulong width = reader.ReadUInt64();
        ulong height = reader.ReadUInt64();
        int pixelCount = reader.ReadInt32();
        byte[] pixels = reader.ReadBytes(pixelCount);

        if (pixels.Length != pixelCount)
        {
            throw new EndOfStreamException("Connection closed before full pixel payload was received.");
        }

        return new FractalResult(width, height, pixels);
    }

    private static Guid ReadGuid(BinaryReader reader)
    {
        return new Guid(reader.ReadBytes(16));
    }

    private static void WriteGuid(BinaryWriter writer, Guid value)
    {
        writer.Write(value.ToByteArray());
    }

    private static MemoryStream CreateReadableStream(ReadOnlyMemory<byte> bytes)
    {
        if (MemoryMarshal.TryGetArray(bytes, out ArraySegment<byte> segment))
        {
            return new MemoryStream(segment.Array!, segment.Offset, segment.Count, writable: false);
        }

        return new MemoryStream(bytes.ToArray(), writable: false);
    }
}
