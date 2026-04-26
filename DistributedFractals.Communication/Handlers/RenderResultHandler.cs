using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Handlers;

public class RenderResultHandler(IFrameResultReceiver receiver) : IMessageHandler<RenderResultMessage>
{
    public Task HandleAsync(RenderResultMessage message)
    {
        receiver.OnResultReceived(message.Sender, message.FrameIndex, message.Result, message.RenderDuration);
        return Task.CompletedTask;
    }
}
