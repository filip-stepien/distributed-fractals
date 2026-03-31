using System.Net;
using DistributedFractals.Core.Core;
using DistributedFractals.Core.Generators.Mandelbrot;
using DistributedFractals.Core.Zoom;
using DistributedFractals.Core.Zoom.Interpolations;
using DistributedFractals.Server.Dispatching;
using DistributedFractals.Server.Handlers.Master;
using DistributedFractals.Server.Heartbeat;
using DistributedFractals.Server.Messages;
using DistributedFractals.Server.Serialization;
using DistributedFractals.Server.Tcp;
using ServerTest;

MandelbrotOptions baseOptions = new(800, 600, MaxIterations: 500);

List<ZoomKeyframe> keyframes =
[
    new ZoomKeyframe(T: 0.0, CenterRe: -0.75,    CenterIm:  0.0,    Scale: 1.0),
    new ZoomKeyframe(T: 0.5, CenterRe: -0.7269,  CenterIm:  0.1889, Scale: 0.05),
    new ZoomKeyframe(T: 1.0, CenterRe: -0.74529, CenterIm:  0.1130, Scale: 0.001),
];

IEnumerable<MandelbrotOptions> zoomFrames = new KeyframeZoomSequenceGenerator<MandelbrotOptions>()
    .Generate(baseOptions, keyframes, totalFrames: 20, new SmoothStepInterpolation());

HeartbeatMessageMasterNode master = new(new TcpMessageNodeFactory(
    IPAddress.Loopback, 3000, new JsonSerializer()
).CreateMasterNode(), TimeSpan.FromSeconds(30));

MessageDispatcher dispatcher = new();
dispatcher.Register(new JoinMessageHandler(master));
dispatcher.Register(new HeartbeatMessageHandler(master));

int framesSaved = 0;
dispatcher.Register(new RenderResultHandler(result =>
{
    int index = Interlocked.Increment(ref framesSaved);
    string path = FractalImageSaver.Save(result, $"frame_{index:D4}");
    Console.WriteLine($"[MASTER] Frame {index} saved: {path}");
}));

master.MessageReceived += async message =>
{
    await dispatcher.DispatchAsync(message);
};

master.WorkerRegistered += async worker =>
{
    Console.WriteLine($"[MASTER] Worker joined: {worker}. Sending {zoomFrames.Count()} frames...");

    int frameIndex = 0;
    foreach (MandelbrotOptions frameOptions in zoomFrames)
    {
        await master.SendToWorkerAsync(worker, new RenderFractalMessage(
            master.Identifier,
            FractalGeneratorType.Mandelbrot,
            FractalColorizerType.BlackAndWhite,
            frameOptions
        ));

        Console.WriteLine($"[MASTER] Frame {++frameIndex} sent.");
    }
};

master.WorkerUnregistered += worker =>
    Console.WriteLine($"[MASTER] Worker unregistered: {worker}");

await master.StartAsync();
Console.WriteLine("[MASTER] Server started...");
Console.ReadLine();

await master.DisposeAsync();
