using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;
using DistributedFractals.Server.Serialization;

namespace DistributedFractals.Server.Udp;

public class UdpServerNode(IPAddress listenAddress, int port, ISerializer serializer) : IMessageMasterNode
{
    public MessageNodeIdentifier Identifier { get; } = new();

    public IReadOnlyCollection<MessageNodeIdentifier> ConnectedWorkers => _connectedWorkers.Values.ToList();

    public event Action<Message>? MessageReceived;

    private readonly ConcurrentDictionary<string, MessageNodeIdentifier> _connectedWorkers = new();
    private readonly ConcurrentDictionary<string, IPEndPoint> _endpoints = new();
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;

    public void RegisterWorker(MessageNodeIdentifier worker)
    {
        _connectedWorkers[worker.Id] = worker;
    }

    public void UnregisterWorker(MessageNodeIdentifier worker)
    {
        _connectedWorkers.TryRemove(worker.Id, out _);
        _endpoints.TryRemove(worker.Id, out _);
    }

    public Task StartAsync()
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

    public ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        _connectedWorkers.Clear();
        _endpoints.Clear();
        _udpClient?.Close();
        _udpClient = null;

        return ValueTask.CompletedTask;
    }

    public async Task SendToWorkerAsync(MessageNodeIdentifier workerIdentifier, Message message)
    {
        if (!_connectedWorkers.ContainsKey(workerIdentifier.Id))
        {
            throw new InvalidOperationException($"Worker '{workerIdentifier.Id}' has not joined.");
        }

        if (!_endpoints.TryGetValue(workerIdentifier.Id, out IPEndPoint? endpoint))
        {
            throw new InvalidOperationException($"Worker '{workerIdentifier.Id}' endpoint is unknown.");
        }

        byte[] data = serializer.Serialize(message).ToArray();
        await _udpClient!.SendAsync(data, endpoint);
    }

    public async Task BroadcastAsync(Message message)
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
            Message message = serializer.Deserialize<Message>(result.Buffer);

            _endpoints.TryAdd(message.Sender.Id, result.RemoteEndPoint);

            MessageReceived?.Invoke(message);
        }
    }
}
