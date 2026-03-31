using DistributedFractals.Core.Core;
using DistributedFractals.Core.Generators.Mandelbrot;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Handlers;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Tasking.Handlers;

public class RenderMandelbrotHandler(
    MandelbrotGenerator generator,
    IFractalColorizer colorizer,
    IMessageClient worker) : IMessageHandler<RenderMandelbrotMessage>
{
    public async Task HandleAsync(RenderMandelbrotMessage message)
    {
        FractalResult result = generator.Generate(message.Options, colorizer);
        await client.SendToMasterAsync(new RenderResultMessage(worker.Identifier, result));
    }
}
