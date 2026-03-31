using DistributedFractals.Core.Core;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Handlers;

public class RenderFractalHandler(
    IMessageClient client,
    IReadOnlyDictionary<FractalGeneratorType, Func<IFractalGeneratorOptions, IFractalColorizer, FractalResult>> generators,
    IReadOnlyDictionary<FractalColorizerType, IFractalColorizer> colorizers
) : IMessageHandler<RenderFractalMessage>
{
    public async Task HandleAsync(RenderFractalMessage message)
    {
        if (!generators.TryGetValue(message.GeneratorType, out var generate))
            throw new InvalidOperationException($"No generator registered for {message.GeneratorType}.");

        if (!colorizers.TryGetValue(message.ColorizerType, out IFractalColorizer? colorizer))
            throw new InvalidOperationException($"No colorizer registered for {message.ColorizerType}.");

        Console.WriteLine($"[WORKER] Rendering frame {message.FrameIndex}...");
        FractalResult result = generate(message.Options, colorizer);
        Console.WriteLine($"[WORKER] Frame {message.FrameIndex} rendered ({result.Width}x{result.Height}). Sending result...");
        await client.SendToServerAsync(new RenderResultMessage(client.Identifier, message.FrameIndex, result));
    }

    public class Builder(IMessageClient client)
    {
        private readonly Dictionary<FractalGeneratorType, Func<IFractalGeneratorOptions, IFractalColorizer, FractalResult>> _generators = new();
        private readonly Dictionary<FractalColorizerType, IFractalColorizer> _colorizers = new();

        public Builder AddGenerator<TOptions>(
            FractalGeneratorType type,
            IFractalGenerator<TOptions> generator
        ) where TOptions : IFractalGeneratorOptions
        {
            _generators[type] = (opts, col) =>
            {
                if (opts is not TOptions typedOpts)
                    throw new InvalidOperationException(
                        $"Expected options of type {typeof(TOptions).Name} for {type}, got {opts.GetType().Name}.");
                return generator.Generate(typedOpts, col);
            };
            return this;
        }

        public Builder AddColorizer(FractalColorizerType type, IFractalColorizer colorizer)
        {
            _colorizers[type] = colorizer;
            return this;
        }

        public RenderFractalHandler Build() => new(client, _generators, _colorizers);
    }
}
