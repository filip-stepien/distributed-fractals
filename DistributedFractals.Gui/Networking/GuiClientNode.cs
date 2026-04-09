using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DistributedFractals.Core.Colorizers;
using DistributedFractals.Core.Core;
using DistributedFractals.Core.Generators.Mandelbrot;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Dispatching;
using DistributedFractals.Server.Handlers;
using DistributedFractals.Server.Messages;
using DistributedFractals.Server.Serialization;
using DistributedFractals.Server.Tcp;

namespace DistributedFractals.Gui.Networking;

/// <summary>
/// GUI-side wrapper for a TCP client node.
/// Connects, sends JoinMessage, runs heartbeat, handles RenderFractalMessage with
/// the configured generators / colorizers, and surfaces lifecycle + per-frame events.
/// </summary>
public sealed class GuiClientNode : IAsyncDisposable
{
    private readonly IMessageClient _client;
    private readonly TimeSpan _heartbeatInterval;
    private readonly string _displayName;
    private readonly CancellationTokenSource _cts = new();
    private Task? _heartbeatLoop;

    public Guid Identifier => _client.Identifier;

    public event Action? Connected;
    public event Action? Disconnected;
    public event Action<int>? FrameStarted;
    public event Action<int, TimeSpan, FractalResult>? FrameCompleted;
    public event Action<int, Exception>? FrameFailed;

    public GuiClientNode(IPAddress address, int port, TimeSpan heartbeatInterval, string displayName = "")
    {
        _client = new TcpTransportFactory(address, port, new JsonSerializer()).CreateClient();
        _heartbeatInterval = heartbeatInterval;
        _displayName = displayName ?? string.Empty;

        var generators = new Dictionary<FractalGeneratorType, IFractalGenerator<MandelbrotOptions>>
        {
            [FractalGeneratorType.Mandelbrot] = new MandelbrotGenerator(),
        };
        var colorizers = new Dictionary<FractalColorizerType, IFractalColorizer>
        {
            [FractalColorizerType.BlackAndWhite] = new BlackAndWhiteColorizer(),
            [FractalColorizerType.CyclingHsv]    = new CyclingHsvColorizer(),
        };

        var dispatcher = new MessageDispatcher();
        dispatcher.Register(new DisconnectNotifyingHandler(this));
        dispatcher.Register(new InstrumentedRenderHandler(this, _client, generators, colorizers));

        _client.MessageReceived += async message => await dispatcher.DispatchAsync(message);
    }

    public async Task StartAsync()
    {
        await _client.StartAsync();
        await _client.SendToServerAsync(new JoinMessage(_client.Identifier, _displayName));
        Connected?.Invoke();
        _heartbeatLoop = Task.Run(HeartbeatLoopAsync);
    }

    private async Task HeartbeatLoopAsync()
    {
        try
        {
            using PeriodicTimer timer = new(_heartbeatInterval);
            while (await timer.WaitForNextTickAsync(_cts.Token))
            {
                await _client.SendToServerAsync(new HeartbeatMessage(_client.Identifier));
            }
        }
        catch (OperationCanceledException) { }
        catch
        {
            Disconnected?.Invoke();
        }
    }

    internal void RaiseDisconnected() => Disconnected?.Invoke();
    internal void RaiseFrameStarted(int frameIndex) => FrameStarted?.Invoke(frameIndex);
    internal void RaiseFrameCompleted(int frameIndex, TimeSpan duration, FractalResult result)
        => FrameCompleted?.Invoke(frameIndex, duration, result);
    internal void RaiseFrameFailed(int frameIndex, Exception ex) => FrameFailed?.Invoke(frameIndex, ex);

    public async ValueTask DisposeAsync()
    {
        if (!_cts.IsCancellationRequested)
            await _cts.CancelAsync();

        if (_heartbeatLoop is not null)
        {
            try { await _heartbeatLoop; } catch { }
        }

        await _client.DisposeAsync();
        _cts.Dispose();
    }

    private sealed class DisconnectNotifyingHandler(GuiClientNode owner)
        : IMessageHandler<UnregisteredMessage>
    {
        public Task HandleAsync(UnregisteredMessage message)
        {
            owner.RaiseDisconnected();
            return Task.CompletedTask;
        }
    }

    private sealed class InstrumentedRenderHandler(
        GuiClientNode owner,
        IMessageClient client,
        IReadOnlyDictionary<FractalGeneratorType, IFractalGenerator<MandelbrotOptions>> generators,
        IReadOnlyDictionary<FractalColorizerType, IFractalColorizer> colorizers
    ) : IMessageHandler<RenderFractalMessage>
    {
        public async Task HandleAsync(RenderFractalMessage message)
        {
            try
            {
                if (!generators.TryGetValue(message.GeneratorType, out var generator))
                    throw new InvalidOperationException($"No generator registered for {message.GeneratorType}.");
                if (!colorizers.TryGetValue(message.ColorizerType, out var colorizer))
                    throw new InvalidOperationException($"No colorizer registered for {message.ColorizerType}.");
                if (message.Options is not MandelbrotOptions options)
                    throw new InvalidOperationException($"Expected MandelbrotOptions, got {message.Options.GetType().Name}.");

                owner.RaiseFrameStarted(message.FrameIndex);

                var sw = Stopwatch.StartNew();
                FractalResult result = await Task.Run(() => generator.Generate(options, colorizer));
                sw.Stop();

                await client.SendToServerAsync(new RenderResultMessage(client.Identifier, message.FrameIndex, result));
                owner.RaiseFrameCompleted(message.FrameIndex, sw.Elapsed, result);
            }
            catch (Exception ex)
            {
                owner.RaiseFrameFailed(message.FrameIndex, ex);
            }
        }
    }
}
