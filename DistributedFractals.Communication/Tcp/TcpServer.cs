using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;
using DistributedFractals.Server.Serialization;

namespace DistributedFractals.Server.Tcp;

public class TcpServer(IPAddress listenAddress, int port, ISerializer serializer) : MessageServerBase
{
    public override event Action<BaseMessage>? MessageReceived;

    private readonly ConcurrentDictionary<Guid, TcpStream> _streams = new();
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;

    public override void UnregisterClient(Guid client)
    {
        _streams.TryRemove(client, out _);
        base.UnregisterClient(client);
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

    public override async Task SendToClientAsync(Guid clientIdentifier, BaseMessage baseMessage)
    {
        if (!Clients.Contains(clientIdentifier))
        {
            throw new InvalidOperationException($"Worker '{clientIdentifier}' is not registered.");
        }

        if (!_streams.TryGetValue(clientIdentifier, out TcpStream? stream))
        {
            throw new InvalidOperationException($"Worker '{clientIdentifier}' stream is unknown.");
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
            System.Net.Sockets.TcpClient client = await _listener!.AcceptTcpClientAsync(cancellationToken);
            TcpStream stream = new(client.GetStream());
            _ = ReceiveLoopAsync(client, stream, cancellationToken);
        }
    }

    private async Task ReceiveLoopAsync(System.Net.Sockets.TcpClient client, TcpStream stream, CancellationToken cancellationToken)
    {
        Guid? clientId = null;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Memory<byte> data = await stream.ReadAsync(cancellationToken);
                BaseMessage baseMessage = serializer.Deserialize<BaseMessage>(data);

                clientId = baseMessage.Sender;
                _streams.TryAdd(clientId.Value, stream);

                MessageReceived?.Invoke(baseMessage);
            }
        }
        catch (Exception)
        {
            // connection dropped - worker remains in Clients:
            // heartbeat timeout is responsible for detecting and unregistering dead workers
            if (clientId.HasValue)
            {
                _streams.TryRemove(clientId.Value, out _);
            }

            client.Close();
        }
    }
}
