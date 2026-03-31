using System.Net;
using DistributedFractals.Core.Colorizers;
using DistributedFractals.Core.Core;
using DistributedFractals.Core.Generators.Mandelbrot;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Dispatching;
using DistributedFractals.Server.Handlers.Worker;
using DistributedFractals.Server.Messages;
using DistributedFractals.Server.Serialization;
using DistributedFractals.Server.Tcp;

IMessageWorkerNode worker = new TcpMessageNodeFactory(
    IPAddress.Loopback, 3000, new JsonSerializer()
).CreateWorkerNode();

MessageDispatcher dispatcher = new();
dispatcher.Register(new UnregisteredMessageHandler());
dispatcher.Register(
    new RenderFractalHandler.Builder(worker)
        .AddGenerator(FractalGeneratorType.Mandelbrot, new MandelbrotGenerator())
        .AddColorizer(FractalColorizerType.BlackAndWhite, new BlackAndWhiteColorizer())
        .Build()
);

worker.MessageReceived += async message =>
{
    await dispatcher.DispatchAsync(message);
};

await worker.StartAsync();
await worker.SendToMasterAsync(new JoinMessage(worker.Identifier));

Console.WriteLine("[WORKER] Joined. Press Enter to quit.");

CancellationTokenSource cts = new();

_ = Task.Run(async () =>
{
    using PeriodicTimer timer = new(TimeSpan.FromSeconds(5));
    while (await timer.WaitForNextTickAsync(cts.Token))
    {
        await worker.SendToMasterAsync(new HeartbeatMessage(worker.Identifier));
        Console.WriteLine("[WORKER] Heartbeat sent.");
    }
}, cts.Token);

Console.ReadLine();

await cts.CancelAsync();
await worker.DisposeAsync();
