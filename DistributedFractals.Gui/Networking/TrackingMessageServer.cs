using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Gui.Networking;

/// <summary>
/// IMessageServer decorator that exposes outgoing RenderFractalMessage
/// dispatches as an event, so the GUI can show "frame X sent to client Y".
/// All other operations forward to the inner server unchanged.
/// </summary>
public sealed class TrackingMessageServer(IMessageServer inner) : IMessageServer
{
    public event Action<Guid, RenderFractalMessage>? RenderFrameSent;

    public Guid Identifier => inner.Identifier;
    public IReadOnlyCollection<Guid> Clients => inner.Clients;

    public event Action<BaseMessage>? MessageReceived
    {
        add => inner.MessageReceived += value;
        remove => inner.MessageReceived -= value;
    }

    public event Action<Guid>? ClientRegistered
    {
        add => inner.ClientRegistered += value;
        remove => inner.ClientRegistered -= value;
    }

    public event Action<Guid>? ClientUnregistered
    {
        add => inner.ClientUnregistered += value;
        remove => inner.ClientUnregistered -= value;
    }

    public void RegisterClient(Guid client) => inner.RegisterClient(client);
    public void UnregisterClient(Guid client) => inner.UnregisterClient(client);

    public string? GetClientAddress(Guid clientId) => inner.GetClientAddress(clientId);

    public Task SendToClientAsync(Guid clientIdentifier, BaseMessage baseMessage)
    {
        if (baseMessage is RenderFractalMessage render)
            RenderFrameSent?.Invoke(clientIdentifier, render);
        return inner.SendToClientAsync(clientIdentifier, baseMessage);
    }

    public Task BroadcastAsync(BaseMessage baseMessage) => inner.BroadcastAsync(baseMessage);

    public Task StartAsync() => inner.StartAsync();

    public ValueTask DisposeAsync() => inner.DisposeAsync();
}
