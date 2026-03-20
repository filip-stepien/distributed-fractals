using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Serialization;

namespace DistributedFractals.Server.Tcp;

public class TcpServerNode(IPAddress listenAddress, int port, ISerializer messageSerializer) : IMessageMasterNode
{
    public event Action<WorkerNodeMessage>? MessageReceived;

    private readonly ConcurrentDictionary<string, TcpClient> _clients = new();
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;

    public Task ConnectAsync()
    {
        if (_listener != null)
        {
            return Task.CompletedTask;
        }

        _cts = new CancellationTokenSource();
        _listener = new TcpListener(listenAddress, port);
        _listener.Start();

        _ = AcceptLoopAsync(_cts.Token);

        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _cts?.Cancel();

        foreach (TcpClient client in _clients.Values)
        {
            client.Close();
        }

        _clients.Clear();
        _listener?.Stop();
        _listener = null;

        return ValueTask.CompletedTask;
    }

    public async Task SendToWorker(MessageNodeIdentifier workerIdentifier, MasterNodeMessage message)
    {
        if (!_clients.TryGetValue(workerIdentifier.Id, out TcpClient? client))
        {
            return;
        }
        
        await client.GetStream().WriteAsync(messageSerializer.Serialize(message));
    }

    public async Task BroadcastToWorkers(MasterNodeMessage message)
    {
        foreach (TcpClient client in _clients.Values)
        {
            NetworkStream stream = client.GetStream();
            await stream.WriteAsync(messageSerializer.Serialize(message));
        }
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        if (_listener is null)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client = await _listener.AcceptTcpClientAsync(cancellationToken);
            _ = ReceiveLoopAsync(client, cancellationToken);
        }
    }

    private async Task ReceiveLoopAsync(TcpClient client, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[4096];
        string? workerId = null;
        NetworkStream stream = client.GetStream();

        while (!cancellationToken.IsCancellationRequested)
        {
            int bytesRead = await stream.ReadAsync(buffer, cancellationToken);
            Memory<byte> serializedMessage = buffer.AsMemory(0, bytesRead);
            WorkerNodeMessage message = messageSerializer.Deserialize<WorkerNodeMessage>(serializedMessage);

            workerId ??= message.Sender.Id;
            _clients[workerId] = client;

            MessageReceived?.Invoke(message);
        }
    }
}
