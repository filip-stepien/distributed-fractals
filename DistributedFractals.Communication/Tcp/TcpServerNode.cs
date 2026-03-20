using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Serialization;

namespace DistributedFractals.Server.Tcp;

public class TcpServerNode(IPAddress listenAddress, int port, ISerializer messageSerializer) : IMessageMasterNode
{
    public event Action<WorkerNodeMessage>? MessageReceived;

    private readonly ConcurrentDictionary<string, TcpStream> _clients = new();
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
        _clients.Clear();
        _listener?.Stop();
        _listener = null;

        return ValueTask.CompletedTask;
    }

    public async Task SendToWorker(MessageNodeIdentifier workerIdentifier, MasterNodeMessage message)
    {
        if (!_clients.TryGetValue(workerIdentifier.Id, out TcpStream? stream))
        {
            return;
        }

        await stream.WriteAsync(messageSerializer.Serialize(message));
    }

    public async Task BroadcastToWorkers(MasterNodeMessage message)
    {
        ReadOnlyMemory<byte> data = messageSerializer.Serialize(message);
        foreach (TcpStream stream in _clients.Values)
        {
            await stream.WriteAsync(data);
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
            TcpStream stream = new(client.GetStream());
            _ = ReceiveLoopAsync(stream, cancellationToken);
        }
    }

    private async Task ReceiveLoopAsync(TcpStream stream, CancellationToken cancellationToken)
    {
        string? workerId = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            Memory<byte> data = await stream.ReadAsync(cancellationToken);
            WorkerNodeMessage message = messageSerializer.Deserialize<WorkerNodeMessage>(data);

            workerId ??= message.Sender.Id;
            _clients[workerId] = stream;

            MessageReceived?.Invoke(message);
        }
    }
}
