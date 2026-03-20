using System.Net;
using System.Net.Sockets;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Serialization;

namespace DistributedFractals.Server.Tcp;

public class TcpClientNode(IPAddress serverAddress, int port, ISerializer messageSerializer) : IMessageWorkerNode
{
    public MessageNodeIdentifier Identifier { get; } = new();
    public event Action<MasterNodeMessage>? MessageReceived;

    private TcpStream? _stream;
    private TcpClient? _client;
    private CancellationTokenSource? _cts;

    public async Task ConnectAsync()
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

    public async Task SendToMaster(WorkerNodeMessage message)
    {
        if (_stream is null)
        {
            throw new InvalidOperationException("Client is not connected.");
        }

        await _stream.WriteAsync(messageSerializer.Serialize(message));
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        if (_stream is null)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            Memory<byte> data = await _stream.ReadAsync(cancellationToken);
            MasterNodeMessage message = messageSerializer.Deserialize<MasterNodeMessage>(data);
            MessageReceived?.Invoke(message);
        }
    }
}
