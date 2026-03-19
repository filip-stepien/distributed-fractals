using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using DistributedFractals.Server.Core;

namespace DistributedFractals.Server.Tcp;

public class TcpServerNode(IPAddress listenAddress, int port) : IMessageNode
{
    public MessageNodeIdentifier Identifier => MessageNodeIdentifier.Master;
    public event Action<Message>? MessageReceived;

    private readonly ConcurrentDictionary<MessageNodeIdentifier, TcpClient> _clients = new();
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        if (_listener is null)
        {
            return;
        }

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync(cancellationToken);
                _ = ReceiveLoopAsync(client, cancellationToken);
            }
        }
        catch (Exception)
        {
            // TODO
        }
    }

    private async Task ReceiveLoopAsync(TcpClient client, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[4096];
        MessageNodeIdentifier? senderId = null;

        try
        {
            NetworkStream stream = client.GetStream();

            while (!cancellationToken.IsCancellationRequested)
            {
                int bytesRead = await stream.ReadAsync(buffer, cancellationToken);

                if (bytesRead == 0)
                {
                    break;
                }

                string json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Message? message = JsonSerializer.Deserialize<Message>(json);

                if (message is null)
                {
                    continue;
                }

                senderId ??= message.Sender;
                _clients[senderId] = client;

                MessageReceived?.Invoke(message);
            }
        }
        catch
        {
            // TODO
        }
        finally
        {
            if (senderId != null)
            {
                _clients.TryRemove(senderId, out _);
            }

            client.Close();
        }
    }
    
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

    public async Task SendAsync(Message message)
    {
        if (!_clients.TryGetValue(message.Receiver, out TcpClient? client))
            return;

        NetworkStream stream = client.GetStream();
        string json = JsonSerializer.Serialize(message);
        byte[] data = Encoding.UTF8.GetBytes(json);

        await stream.WriteAsync(data);
    }
}