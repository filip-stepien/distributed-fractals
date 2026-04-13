using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;
using DistributedFractals.Server.Serializers;

namespace DistributedFractals.Server.Tcp;

public class TcpServer(IPAddress listenAddress, int port, ISerializer serializer) : MessageServerBase
{
    public override event Action<BaseMessage>? MessageReceived;

    private readonly ConcurrentDictionary<Guid, TcpStream> _streams = new();
    private readonly ConcurrentDictionary<Guid, string> _addresses = new();
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;

    public override void UnregisterClient(ClientIdentifier client)
    {
        _streams.TryRemove(client.Id, out _);
        _addresses.TryRemove(client.Id, out _);
        base.UnregisterClient(client);
    }

    public override string? GetClientAddress(Guid clientId)
        => _addresses.TryGetValue(clientId, out string? address) ? address : null;

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

    public override async Task SendToClientAsync(ClientIdentifier client, BaseMessage message)
    {
        if (!Clients.Contains(client))
        {
            throw new InvalidOperationException($"Client '{client.Id}' is not registered.");
        }

        if (!_streams.TryGetValue(client.Id, out TcpStream? stream))
        {
            throw new InvalidOperationException($"Client '{client.Id}' stream is unknown.");
        }

        await stream.WriteAsync(serializer.Serialize(message));
    }

    public override async Task BroadcastAsync(BaseMessage message)
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
            System.Net.Sockets.TcpClient client = await _listener!.AcceptTcpClientAsync(cancellationToken);
            TcpStream stream = new(client.GetStream());
            string? remoteAddress = (client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString();
            _ = ReceiveLoopAsync(client, stream, remoteAddress, cancellationToken);
        }
    }

    private async Task ReceiveLoopAsync(System.Net.Sockets.TcpClient client, TcpStream stream, string? remoteAddress, CancellationToken cancellationToken)
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
                if (remoteAddress is not null)
                    _addresses.TryAdd(clientId.Value, remoteAddress);

                MessageReceived?.Invoke(baseMessage);
            }
        }
        catch (Exception)
        {
            // connection dropped - client remains in Clients:
            // heartbeat timeout is responsible for detecting and unregistering dead clients
            if (clientId.HasValue)
            {
                _streams.TryRemove(clientId.Value, out _);
            }

            client.Close();
        }
    }
}
