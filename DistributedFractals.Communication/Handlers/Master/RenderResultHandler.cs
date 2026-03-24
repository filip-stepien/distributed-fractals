using DistributedFractals.Core.Core;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Handlers.Master;

public class RenderResultHandler(Action<FractalResult> onResult) : IMessageHandler<RenderResultMessage>
{
    public Task HandleAsync(RenderResultMessage message)
    {
        onResult(message.Result);
        return Task.CompletedTask;
    }
}
