using System.Net;
using DistributedFractals.Core.Colorizers;
using DistributedFractals.Core.Core;
using DistributedFractals.Core.Generators.Mandelbrot;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Dispatching;
using DistributedFractals.Server.Handlers;
using DistributedFractals.Server.Messages;
using DistributedFractals.Server.Serialization;
using DistributedFractals.Server.Tcp;

IMessageClient client = new TcpTransportFactory(
    IPAddress.Loopback, 3000, new JsonSerializer()
).CreateClient();

MessageDispatcher dispatcher = new();
dispatcher.Register(new UnregisteredMessageHandler());
dispatcher.Register(
    new RenderFractalHandler.Builder(client)
        .AddGenerator(FractalGeneratorType.Mandelbrot, new MandelbrotGenerator())
        .AddColorizer(FractalColorizerType.BlackAndWhite, new BlackAndWhiteColorizer())
        .AddColorizer(FractalColorizerType.CyclingHsv, new CyclingHsvColorizer())
        .Build()
);

client.MessageReceived += async message =>
{
    await dispatcher.DispatchAsync(message);
};

await client.StartAsync();
await client.SendToServerAsync(new JoinMessage(client.Identifier));

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
