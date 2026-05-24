using DistributedFractals.Fractal;
using DistributedFractals.Fractal.Core;
using DistributedFractals.Logging;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Handlers;

public class RenderFractalHandler(
    IMessageClient client,
    Action<int>? onStarted = null,
    Action<int, TimeSpan, FractalResult>? onCompleted = null,
    Action<int>? onFailed = null
) : IMessageHandler<RenderFrameMessage>, IMessageHandler<RenderBatchMessage>
{
    public async Task HandleAsync(RenderFrameMessage message)
    {
        RenderFrameResult result = await RenderFrameAsync(message);
        await client.SendToServerAsync(new RenderResultMessage(client.Identifier, message.RenderJobId, result.FrameIndex, result.Result, result.RenderDuration));
        onCompleted?.Invoke(result.FrameIndex, result.RenderDuration, result.Result);
    }

    public async Task HandleAsync(RenderBatchMessage message)
    {
        Logger.Log($"Rendering batch {message.BatchId} ({message.Frames.Count} frames)...");
        DateTime batchStart = DateTime.UtcNow;
        List<RenderFrameResult> results = new(message.Frames.Count);

        foreach (RenderFrameMessage frame in message.Frames)
        {
            results.Add(await RenderFrameAsync(frame));
        }

        TimeSpan batchDuration = DateTime.UtcNow - batchStart;
        Logger.Log($"Batch {message.BatchId} rendered ({results.Count} frames). Sending results...");

        await client.SendToServerAsync(new RenderBatchResultMessage(client.Identifier, message.RenderJobId, message.BatchId, results, batchDuration));

        foreach (RenderFrameResult result in results)
        {
            onCompleted?.Invoke(result.FrameIndex, result.RenderDuration, result.Result);
        }
    }

    private async Task<RenderFrameResult> RenderFrameAsync(RenderFrameMessage message)
    {
        onStarted?.Invoke(message.FrameIndex);
        try
        {
            Logger.Log($"Rendering frame {message.FrameIndex}...");
            DateTime start = DateTime.UtcNow;
            FractalResult result = await FrameRenderer.RenderAsync(message.Options, message.Bounds, message.ColorizerType);
            TimeSpan duration = DateTime.UtcNow - start;
            Logger.Log($"Frame {message.FrameIndex} rendered ({result.Width}x{result.Height}).");

            return new RenderFrameResult(message.FrameIndex, result, duration);
        }
        catch (Exception)
        {
            onFailed?.Invoke(message.FrameIndex);
            throw;
        }
    }
}
