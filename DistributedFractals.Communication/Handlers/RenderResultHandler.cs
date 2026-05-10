using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Handlers;

public class RenderResultHandler(IFrameResultReceiver receiver) :
    IMessageHandler<RenderResultMessage>,
    IMessageHandler<RenderBatchResultMessage>
{
    public Task HandleAsync(RenderResultMessage message)
    {
        receiver.OnResultReceived(message.Sender, message.FrameIndex, message.Result, message.RenderDuration);
        return Task.CompletedTask;
    }

    public Task HandleAsync(RenderBatchResultMessage message)
    {
        receiver.OnBatchResultReceived(message.Sender, message.BatchId, message.Results, message.RenderDuration);
        return Task.CompletedTask;
    }
}
