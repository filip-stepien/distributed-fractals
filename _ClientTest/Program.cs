using System.Net;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Dispatchers;
using DistributedFractals.Server.Handlers;
using DistributedFractals.Server.Messages;
using DistributedFractals.Server.Serializers;
using DistributedFractals.Server.Tcp;

IMessageClient client = new TcpTransportFactory(
    IPAddress.Loopback, 3000, new JsonSerializer()
).CreateClient();

MessageDispatcher dispatcher = new();
dispatcher.Register(new UnregisteredMessageHandler());
dispatcher.Register(new RenderFractalHandler(client));

client.MessageReceived += async message =>
{
    await dispatcher.DispatchAsync(message);
};

await client.StartAsync();
await client.SendToServerAsync(new JoinMessage(client.Identifier, "TestClient"));

Console.WriteLine("[WORKER] Joined. Press Enter to quit.");

CancellationTokenSource cts = new();

_ = Task.Run(async () =>
{
    using PeriodicTimer timer = new(TimeSpan.FromSeconds(5));
    while (await timer.WaitForNextTickAsync(cts.Token))
    {
        await client.SendToServerAsync(new HeartbeatMessage(client.Identifier));
        Console.WriteLine("[WORKER] Heartbeat sent.");
    }
}, cts.Token);

Console.ReadLine();

await cts.CancelAsync();
await client.DisposeAsync();
