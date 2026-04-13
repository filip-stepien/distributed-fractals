using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Gui.Networking;

/// <summary>
/// IMessageServer decorator that exposes outgoing RenderFrameMessage
/// dispatches as an event, so the GUI can show "frame X sent to client Y".
/// All other operations forward to the inner server unchanged.
/// </summary>
public sealed class TrackingMessageServer(IMessageServer inner) : IMessageServer
{
    public event Action<ClientIdentifier, RenderFrameMessage>? RenderFrameSent;

    public Guid Identifier => inner.Identifier;
    public IReadOnlyCollection<ClientIdentifier> Clients => inner.Clients;

    public event Action<BaseMessage>? MessageReceived
    {
        add => inner.MessageReceived += value;
        remove => inner.MessageReceived -= value;
    }

    public event Action<ClientIdentifier>? ClientRegistered
    {
        add => inner.ClientRegistered += value;
        remove => inner.ClientRegistered -= value;
    }

    public event Action<ClientIdentifier>? ClientUnregistered
    {
        add => inner.ClientUnregistered += value;
        remove => inner.ClientUnregistered -= value;
    }

    public void RegisterClient(ClientIdentifier client) => inner.RegisterClient(client);
    public void UnregisterClient(ClientIdentifier client) => inner.UnregisterClient(client);

    public string? GetClientAddress(Guid clientId) => inner.GetClientAddress(clientId);

    public Task SendToClientAsync(ClientIdentifier client, BaseMessage message)
    {
        if (message is RenderFrameMessage render)
            RenderFrameSent?.Invoke(client, render);
        return inner.SendToClientAsync(client, message);
    }

    public Task BroadcastAsync(BaseMessage message) => inner.BroadcastAsync(message);

    public Task StartAsync() => inner.StartAsync();

    public ValueTask DisposeAsync() => inner.DisposeAsync();
}
