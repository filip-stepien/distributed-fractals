using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;
using DistributedFractals.Server.Serializers;

namespace DistributedFractals.Server.Udp;

public class UdpServer(IPAddress listenAddress, int port, ISerializer serializer) : MessageServerBase
{
    public override event Action<BaseMessage>? MessageReceived;

    private readonly ConcurrentDictionary<Guid, IPEndPoint> _endpoints = new();
    private System.Net.Sockets.UdpClient? _udpClient;
    private CancellationTokenSource? _cts;

    public override void UnregisterClient(ClientIdentifier client)
    {
        _endpoints.TryRemove(client.Id, out _);
        base.UnregisterClient(client);
    }

    public override Task StartAsync()
    {
        if (_udpClient != null)
        {
            return Task.CompletedTask;
        }

        _cts = new CancellationTokenSource();
        _udpClient = new System.Net.Sockets.UdpClient(new IPEndPoint(listenAddress, port));

        _ = ReceiveLoopAsync(_cts.Token);

        return Task.CompletedTask;
    }

    public override ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        _endpoints.Clear();
        _udpClient?.Close();
        _udpClient = null;

        return ValueTask.CompletedTask;
    }

    public override async Task SendToClientAsync(ClientIdentifier client, BaseMessage message)
    {
        if (!Clients.Contains(client))
        {
            throw new InvalidOperationException($"Client '{client.Id}' is not registered.");
        }

        if (!_endpoints.TryGetValue(client.Id, out IPEndPoint? endpoint))
        {
            throw new InvalidOperationException($"Client '{client.Id}' endpoint is unknown.");
        }

        byte[] data = serializer.Serialize(message).ToArray();
        await _udpClient!.SendAsync(data, endpoint);
    }

    public override async Task BroadcastAsync(BaseMessage message)
    {
        byte[] data = serializer.Serialize(message).ToArray();
        foreach (IPEndPoint endpoint in _endpoints.Values)
        {
            await _udpClient!.SendAsync(data, endpoint);
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            UdpReceiveResult result = await _udpClient!.ReceiveAsync(cancellationToken);
            BaseMessage baseMessage = serializer.Deserialize<BaseMessage>(result.Buffer);

            _endpoints.TryAdd(baseMessage.Sender, result.RemoteEndPoint);

            MessageReceived?.Invoke(baseMessage);
        }
    }
}
