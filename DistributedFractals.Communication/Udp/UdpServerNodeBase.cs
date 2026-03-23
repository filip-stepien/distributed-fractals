using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;
using DistributedFractals.Server.Serialization;

namespace DistributedFractals.Server.Udp;

public class UdpServerNodeBase(IPAddress listenAddress, int port, ISerializer serializer) : MessageMasterNodeBase
{
    public override event Action<BaseMessage>? MessageReceived;

    private readonly ConcurrentDictionary<Guid, IPEndPoint> _endpoints = new();
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;

    public override void UnregisterWorker(Guid worker)
    {
        _endpoints.TryRemove(worker, out _);
        base.UnregisterWorker(worker);
    }

    public override Task StartAsync()
    {
        if (_udpClient != null)
        {
            return Task.CompletedTask;
        }

        _cts = new CancellationTokenSource();
        _udpClient = new UdpClient(new IPEndPoint(listenAddress, port));

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

    public override async Task SendToWorkerAsync(Guid workerIdentifier, BaseMessage baseMessage)
    {
        if (!Workers.Contains(workerIdentifier))
        {
            throw new InvalidOperationException($"Worker '{workerIdentifier}' is not registered.");
        }

        if (!_endpoints.TryGetValue(workerIdentifier, out IPEndPoint? endpoint))
        {
            throw new InvalidOperationException($"Worker '{workerIdentifier}' endpoint is unknown.");
        }

        byte[] data = serializer.Serialize(baseMessage).ToArray();
        await _udpClient!.SendAsync(data, endpoint);
    }

    public override async Task BroadcastAsync(BaseMessage baseMessage)
    {
        byte[] data = serializer.Serialize(baseMessage).ToArray();
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
