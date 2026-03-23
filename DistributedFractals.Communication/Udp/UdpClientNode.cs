using System.Net;
using System.Net.Sockets;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;
using DistributedFractals.Server.Serialization;

namespace DistributedFractals.Server.Udp;

public class UdpClientNode(IPAddress serverAddress, int serverPort, ISerializer serializer) : IMessageWorkerNode
{
    public Guid Identifier { get; } = Guid.NewGuid();

    public event Action<BaseMessage>? MessageReceived;

    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;

    public Task StartAsync()
    {
        if (_udpClient != null)
        {
            return Task.CompletedTask;
        }

        _cts = new CancellationTokenSource();
        _udpClient = new UdpClient();
        _udpClient.Connect(serverAddress, serverPort);

        _ = ReceiveLoopAsync(_cts.Token);

        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        _udpClient?.Close();
        _udpClient = null;

        return ValueTask.CompletedTask;
    }

    public async Task SendToMasterAsync(BaseMessage baseMessage)
    {
        if (_udpClient is null)
        {
            throw new InvalidOperationException("Client is not started.");
        }

        byte[] data = serializer.Serialize(baseMessage).ToArray();
        await _udpClient.SendAsync(data);
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            UdpReceiveResult result = await _udpClient!.ReceiveAsync(cancellationToken);
            MessageReceived?.Invoke(serializer.Deserialize<BaseMessage>(result.Buffer));
        }
    }
}
