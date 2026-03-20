using System.Net;
using System.Net.Sockets;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Serialization;

namespace DistributedFractals.Server.Tcp;

public class TcpClientNode(IPAddress serverAddress, int port, ISerializer messageSerializer) : IMessageWorkerNode
{
    
    public MessageNodeIdentifier Identifier { get; } = new();
    public event Action<MasterNodeMessage>? MessageReceived;

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

        _ = ReceiveLoopAsync(_cts.Token);
    }

    public ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        _client?.Close();
        _client = null;

        return ValueTask.CompletedTask;
    }

    public async Task SendToMaster(WorkerNodeMessage message)
    {
        if (_client is null)
        {
            throw new InvalidOperationException("Client is not connected.");
        }

        await _client.GetStream().WriteAsync(messageSerializer.Serialize(message));
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        if (_client is null)
        {
            return;
        }

        byte[] buffer = new byte[4096];
        NetworkStream stream = _client.GetStream();

        while (!cancellationToken.IsCancellationRequested)
        {
            int bytesRead = await stream.ReadAsync(buffer, cancellationToken);
            Memory<byte> serializedMessage = buffer.AsMemory(0, bytesRead);
            MasterNodeMessage message = messageSerializer.Deserialize<MasterNodeMessage>(serializedMessage);
            MessageReceived?.Invoke(message);
        }
    }
}
