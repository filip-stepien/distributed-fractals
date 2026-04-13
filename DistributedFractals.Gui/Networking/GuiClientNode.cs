using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DistributedFractals.Fractal.Core;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Dispatchers;
using DistributedFractals.Server.Messages;
using DistributedFractals.Server.Serializers;
using DistributedFractals.Server.Tcp;

namespace DistributedFractals.Gui.Networking;

/// <summary>
/// GUI-side wrapper for a TCP client node.
/// Connects, sends JoinMessage, runs heartbeat, handles RenderFrameMessage with
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

        var dispatcher = MessageDispatcherFactory.CreateClient(
            _client,
            onFrameStarted: frameIndex => FrameStarted?.Invoke(frameIndex),
            onFrameCompleted: (frameIndex, duration, result) => FrameCompleted?.Invoke(frameIndex, duration, result),
            onFrameFailed: frameIndex => FrameFailed?.Invoke(frameIndex, new Exception($"Frame {frameIndex} failed")),
            onDisconnected: () => Disconnected?.Invoke()
        );

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
}
