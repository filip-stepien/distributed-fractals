using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using DistributedFractals.Fractal.Core;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Dispatchers;
using DistributedFractals.Server.Handlers;
using DistributedFractals.Server.Heartbeat;
using DistributedFractals.Server.Messages;
using DistributedFractals.Server.Serializers;
using DistributedFractals.Server.Tcp;

namespace DistributedFractals.Gui.Networking;

/// <summary>
/// GUI-side wrapper for a TCP server node with heartbeat tracking.
/// Handles JoinMessage / HeartbeatMessage dispatch and forwards client
/// register / unregister events for the UI to consume.
/// Also forwards RenderResultMessage to the currently active render job (if any).
/// </summary>
public sealed class GuiServerNode : IAsyncDisposable
{
    public HeartbeatMessageServer Server { get; }

    public event Action<ClientIdentifier>? ClientRegistered;
    public event Action<ClientIdentifier>? ClientUnregistered;

    private IFrameResultReceiver? _currentRenderReceiver;
    private readonly ConcurrentDictionary<Guid, string> _displayNames = new();

    public GuiServerNode(IPAddress address, int port, TimeSpan heartbeatTimeout)
    {
        var inner = new TcpTransportFactory(address, port, new JsonSerializer()).CreateServer();
        Server = new HeartbeatMessageServer(inner, heartbeatTimeout);

        var dispatcher = new MessageDispatcher();
        // Capture display name BEFORE the standard JoinMessageHandler fires ClientRegistered,
        // so listeners can look it up via GetDisplayName() in their event callback.
        dispatcher.Register(new DisplayNameCaptureHandler(this));
        dispatcher.Register(new JoinMessageHandler(Server));
        dispatcher.Register(new HeartbeatMessageHandler(Server));
        dispatcher.Register(new RenderResultForwardingHandler(this));

        Server.ClientUnregistered += id => _displayNames.TryRemove(id.Id, out _);

        Server.MessageReceived += async message => await dispatcher.DispatchAsync(message);
        Server.ClientRegistered   += id => ClientRegistered?.Invoke(id);
        Server.ClientUnregistered += id => ClientUnregistered?.Invoke(id);
    }

    public Task StartAsync() => Server.StartAsync();

    public ValueTask DisposeAsync() => Server.DisposeAsync();

    /// <summary>Sets the currently-active render job. RenderResultMessage instances are routed here.</summary>
    internal void SetCurrentRenderReceiver(IFrameResultReceiver? receiver) => _currentRenderReceiver = receiver;

    /// <summary>Returns the display name a client supplied in its JoinMessage, or null if none.</summary>
    public string? GetDisplayName(ClientIdentifier client)
        => _displayNames.TryGetValue(client.Id, out string? name) ? name : null;

    private sealed class DisplayNameCaptureHandler(GuiServerNode owner) : IMessageHandler<JoinMessage>
    {
        public Task HandleAsync(JoinMessage message)
        {
            if (!string.IsNullOrWhiteSpace(message.DisplayName))
                owner._displayNames[message.Sender] = message.DisplayName;
            return Task.CompletedTask;
        }
    }

    private sealed class RenderResultForwardingHandler(GuiServerNode owner) : IMessageHandler<RenderResultMessage>
    {
        public Task HandleAsync(RenderResultMessage message)
        {
            owner._currentRenderReceiver?.OnResultReceived(message.Sender, message.FrameIndex, message.Result, message.RenderDuration);
            return Task.CompletedTask;
        }
    }
}
