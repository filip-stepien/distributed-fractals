using System.Net;
using DistributedFractals.Core.Core;
using DistributedFractals.Core.Generators.Mandelbrot;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Dispatching;
using DistributedFractals.Server.Handlers.Master;
using DistributedFractals.Server.Heartbeat;
using DistributedFractals.Server.Messages;
using DistributedFractals.Server.Serialization;
using DistributedFractals.Server.Tcp;
using ServerTest;

HeartbeatMessageMasterNode master = new(new TcpMessageNodeFactory(
    IPAddress.Loopback, 3000, new JsonSerializer()
).CreateMasterNode(), TimeSpan.FromSeconds(5));

MessageDispatcher dispatcher = new();
dispatcher.Register(new JoinMessageHandler(master));
dispatcher.Register(new HeartbeatMessageHandler(master));
dispatcher.Register(new RenderResultHandler(result =>
{
    string path = FractalImageSaver.Save(result);
    Console.WriteLine($"[MASTER] Image saved: {path}");
}));

master.MessageReceived += async message =>
{
    await dispatcher.DispatchAsync(message);
};

master.WorkerRegistered += async worker =>
{
    await master.SendToWorkerAsync(worker, new RenderFractalMessage(
        master.Identifier,
        FractalGeneratorType.Mandelbrot,
        FractalColorizerType.BlackAndWhite,
        new MandelbrotOptions(800, 600)
    ));

    Console.WriteLine($"[MASTER] Render task sent to {worker}");
};

master.WorkerUnregistered += worker =>
    Console.WriteLine($"[MASTER] Worker unregistered (heartbeat timeout): {worker}");

await master.StartAsync();

Console.WriteLine("[MASTER] Server started...");
Console.ReadLine();

await master.DisposeAsync();
