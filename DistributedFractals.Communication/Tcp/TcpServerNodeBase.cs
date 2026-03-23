using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;
using DistributedFractals.Server.Serialization;

namespace DistributedFractals.Server.Tcp;

public class TcpServerNodeBase(IPAddress listenAddress, int port, ISerializer serializer) : MessageMasterNodeBase
{
    public override event Action<BaseMessage>? MessageReceived;

    private readonly ConcurrentDictionary<Guid, TcpStream> _streams = new();
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;

    public override void UnregisterWorker(Guid worker)
    {
        _streams.TryRemove(worker, out _);
        base.UnregisterWorker(worker);
    }

    public override Task StartAsync()
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

    public override ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        _streams.Clear();
        _listener?.Stop();
        _listener = null;

        return ValueTask.CompletedTask;
    }

    public override async Task SendToWorkerAsync(Guid workerIdentifier, BaseMessage baseMessage)
    {
        if (!Workers.Contains(workerIdentifier))
        {
            throw new InvalidOperationException($"Worker '{workerIdentifier}' is not registered.");
        }

        if (!_streams.TryGetValue(workerIdentifier, out TcpStream? stream))
        {
            throw new InvalidOperationException($"Worker '{workerIdentifier}' stream is unknown.");
        }

        await stream.WriteAsync(serializer.Serialize(baseMessage));
    }

    public override async Task BroadcastAsync(BaseMessage baseMessage)
    {
        ReadOnlyMemory<byte> data = serializer.Serialize(baseMessage);
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
        Guid? workerId = null;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Memory<byte> data = await stream.ReadAsync(cancellationToken);
                BaseMessage baseMessage = serializer.Deserialize<BaseMessage>(data);

                workerId = baseMessage.Sender;
                _streams.TryAdd(workerId.Value, stream);

                MessageReceived?.Invoke(baseMessage);
            }
        }
        catch (Exception)
        {
            // connection dropped - worker remains in Workers:
            // heartbeat timeout is responsible for detecting and unregistering dead workers
            if (workerId.HasValue)
            {
                _streams.TryRemove(workerId.Value, out _);
            }

            client.Close();
        }
    }
}
