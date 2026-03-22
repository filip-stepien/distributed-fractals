using System.Net;
using System.Net.Sockets;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;
using DistributedFractals.Server.Serialization;

namespace DistributedFractals.Server.Tcp;

public class TcpClientNode(IPAddress serverAddress, int port, ISerializer serializer) : IMessageWorkerNode
{
    public MessageNodeIdentifier Identifier { get; } = new();

    public event Action<Message>? MessageReceived;

    private TcpStream? _stream;
    private TcpClient? _client;
    private CancellationTokenSource? _cts;

    public async Task StartAsync()
    {
        if (_client is not null)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _client = new TcpClient();

        await _client.ConnectAsync(serverAddress, port);

        _stream = new TcpStream(_client.GetStream());
        _ = ReceiveLoopAsync(_cts.Token);
    }

    public ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        _client?.Close();
        _client = null;
        _stream = null;

        return ValueTask.CompletedTask;
    }

    public async Task SendAsync(Message message)
    {
        if (_stream is null)
        {
            throw new InvalidOperationException("Client is not connected.");
        }

        await _stream.WriteAsync(serializer.Serialize(message));
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            Memory<byte> data = await _stream!.ReadAsync(cancellationToken);
            MessageReceived?.Invoke(serializer.Deserialize<Message>(data));
        }
    }
}
