using DistributedFractals.Core.Core;
using DistributedFractals.Core.Zoom;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Handlers;

public class RenderFractalHandler(
    IMessageClient client,
    IReadOnlyDictionary<FractalGeneratorType, Func<IFractalGeneratorOptions, FrameBounds, IFractalColorizer, FractalResult>> generators,
    IReadOnlyDictionary<FractalColorizerType, IFractalColorizer> colorizers,
    Action<int>? onStarted = null,
    Action<int, TimeSpan, FractalResult>? onCompleted = null,
    Action<int>? onFailed = null
) : IMessageHandler<RenderFrameMessage>
{
    public async Task HandleAsync(RenderFrameMessage message)
    {
        if (!generators.TryGetValue(message.GeneratorType, out Func<IFractalGeneratorOptions, FrameBounds, IFractalColorizer, FractalResult>? generate))
        {
            throw new InvalidOperationException($"No generator registered for {message.GeneratorType}.");
        }

        if (!colorizers.TryGetValue(message.ColorizerType, out IFractalColorizer? colorizer))
        {
            throw new InvalidOperationException($"No colorizer registered for {message.ColorizerType}.");
        }

        onStarted?.Invoke(message.FrameIndex);
        try
        {
            Console.WriteLine($"[WORKER] Rendering frame {message.FrameIndex}...");
            DateTime start = DateTime.UtcNow;
            FractalResult result = await Task.Run(() => generate(message.Options, message.Bounds, colorizer));
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

    public class Builder(IMessageClient client)
    {
        private readonly Dictionary<FractalGeneratorType, Func<IFractalGeneratorOptions, FrameBounds, IFractalColorizer, FractalResult>> _generators = new();
        private readonly Dictionary<FractalColorizerType, IFractalColorizer> _colorizers = new();
        private Action<int>? _onStarted;
        private Action<int, TimeSpan, FractalResult>? _onCompleted;
        private Action<int>? _onFailed;

        public Builder AddGenerator<TOptions>(
            FractalGeneratorType type,
            IFractalGenerator<TOptions> generator
        ) where TOptions : IFractalGeneratorOptions
        {
            _generators[type] = (options, bounds, colorizer) =>
            {
                if (options is not TOptions typedOptions)
                {
                    throw new InvalidOperationException(
                        $"Expected options of type {typeof(TOptions).Name} for {type}, got {options.GetType().Name}.");
                }
                return generator.Generate(typedOptions, bounds, colorizer);
            };
            return this;
        }

        public Builder AddColorizer(FractalColorizerType type, IFractalColorizer colorizer)
        {
            _colorizers[type] = colorizer;
            return this;
        }

        public Builder OnStarted(Action<int> callback)
        {
            _onStarted = callback;
            return this;
        }

        public Builder OnCompleted(Action<int, TimeSpan, FractalResult> callback)
        {
            _onCompleted = callback;
            return this;
        }

        public Builder OnFailed(Action<int> callback)
        {
            _onFailed = callback;
            return this;
        }

        public RenderFractalHandler Build() =>
            new(client, _generators, _colorizers, _onStarted, _onCompleted, _onFailed);
    }
}
