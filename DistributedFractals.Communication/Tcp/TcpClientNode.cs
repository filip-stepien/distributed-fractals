using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using DistributedFractals.Server.Core;

namespace DistributedFractals.Server.Tcp;

public class TcpClientNode(IPAddress serverAddress, int port) : IMessageNode
{
    public MessageNodeIdentifier Identifier { get; } = new();
    public event Action<Message>? MessageReceived;

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

    public async Task SendAsync(Message message)
    {
        if (_client is null)
        {
            throw new InvalidOperationException("Client is not started.");
        }

        NetworkStream stream = _client.GetStream();
        string json = JsonSerializer.Serialize(message);
        byte[] data = Encoding.UTF8.GetBytes(json);

        await stream.WriteAsync(data);
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        if (_client is null)
        {
            return;
        }

        byte[] buffer = new byte[4096];

        try
        {
            NetworkStream stream = _client.GetStream();

            while (!cancellationToken.IsCancellationRequested)
            {
                int bytesRead = await stream.ReadAsync(buffer, cancellationToken);

                if (bytesRead == 0)
                {
                    break;
                }

                string json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Message? message = JsonSerializer.Deserialize<Message>(json);

                if (message is not null)
                {
                    MessageReceived?.Invoke(message);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }
}