using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;
using DistributedFractals.Server.Serialization;

namespace DistributedFractals.Server.Tcp;

public class TcpServerNode(IPAddress listenAddress, int port, ISerializer serializer) : IMessageMasterNode
{
    public MessageNodeIdentifier Identifier { get; } = new();

    public IReadOnlyCollection<MessageNodeIdentifier> ConnectedWorkers => _connectedWorkers.Values.ToList();

    public event Action<Message>? MessageReceived;

    private readonly ConcurrentDictionary<string, MessageNodeIdentifier> _connectedWorkers = new();
    private readonly ConcurrentDictionary<string, TcpStream> _streams = new();
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;

    public void RegisterWorker(MessageNodeIdentifier worker)
    {
        _connectedWorkers[worker.Id] = worker;
    }

    public void UnregisterWorker(MessageNodeIdentifier worker)
    {
        _connectedWorkers.TryRemove(worker.Id, out _);
        _streams.TryRemove(worker.Id, out _);
    }

    public Task StartAsync()
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
        _connectedWorkers.Clear();
        _streams.Clear();
        _listener?.Stop();
        _listener = null;

        return ValueTask.CompletedTask;
    }

    public async Task SendToWorkerAsync(MessageNodeIdentifier workerIdentifier, Message message)
    {
        if (!_connectedWorkers.ContainsKey(workerIdentifier.Id))
        {
            throw new InvalidOperationException($"Worker '{workerIdentifier.Id}' has not joined.");
        }
        
        if (!_streams.TryGetValue(workerIdentifier.Id, out TcpStream? stream))
        {
            throw new InvalidOperationException($"Worker '{workerIdentifier.Id}' stream is unknown.");
        }

        await stream.WriteAsync(serializer.Serialize(message));
    }

    public async Task BroadcastAsync(Message message)
    {
        ReadOnlyMemory<byte> data = serializer.Serialize(message);
        foreach (TcpStream stream in _streams.Values)
        {
            await stream.WriteAsync(data);
        }
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client = await _listener!.AcceptTcpClientAsync(cancellationToken);
            TcpStream stream = new(client.GetStream());
            _ = ReceiveLoopAsync(client, stream, cancellationToken);
        }
    }

    private async Task ReceiveLoopAsync(TcpClient client, TcpStream stream, CancellationToken cancellationToken)
    {
        string? workerId = null;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Memory<byte> data = await stream.ReadAsync(cancellationToken);
                Message message = serializer.Deserialize<Message>(data);

                workerId = message.Sender.Id;
                _streams.TryAdd(workerId, stream);

                MessageReceived?.Invoke(message);
            }
        }
        catch (Exception)
        {
            // connection dropped - worker remains in _connectedWorkers:
            // heartbeat timeout is responsible for detecting and unregistering dead workers
            if (workerId != null)
            {
                _streams.TryRemove(workerId, out _);
            }

            client.Close();
        }
    }
}
