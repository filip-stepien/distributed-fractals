using DistributedFractals.Fractal;
using DistributedFractals.Fractal.Core;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Handlers;

public class RenderFractalHandler(
    IMessageClient client,
    Action<int>? onStarted = null,
    Action<int, TimeSpan, FractalResult>? onCompleted = null,
    Action<int>? onFailed = null
) : IMessageHandler<RenderFrameMessage>
{
    public async Task HandleAsync(RenderFrameMessage message)
    {
        onStarted?.Invoke(message.FrameIndex);
        try
        {
            Console.WriteLine($"[WORKER] Rendering frame {message.FrameIndex}...");
            DateTime start = DateTime.UtcNow;
            FractalResult result = await FrameRenderer.RenderAsync(message.Options, message.Bounds, message.ColorizerType);
            TimeSpan duration = DateTime.UtcNow - start;
            Console.WriteLine($"[WORKER] Frame {message.FrameIndex} rendered ({result.Width}x{result.Height}). Sending result...");

            await client.SendToServerAsync(new RenderResultMessage(client.Identifier, message.FrameIndex, result));
            onCompleted?.Invoke(message.FrameIndex, duration, result);
        }
        catch (Exception)
        {
            onFailed?.Invoke(message.FrameIndex);
            throw;
        }
    }
}
