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
using DistributedFractals.Video.Gif;

MandelbrotOptions baseOptions = new(400, 300, MaxIterations: 500);

List<ZoomKeyframe> keyframes =
[
    new ZoomKeyframe(T: 0.0, CenterRe: -0.75,    CenterIm:  0.0,    Scale: 1.0),
    new ZoomKeyframe(T: 0.5, CenterRe: -0.7269,  CenterIm:  0.1889, Scale: 0.05),
    new ZoomKeyframe(T: 1.0, CenterRe: -0.74529, CenterIm:  0.1130, Scale: 0.001),
];

IEnumerable<MandelbrotOptions> zoomFrames = new KeyframeZoomSequenceGenerator<MandelbrotOptions>()
    .Generate(baseOptions, keyframes, totalFrames: 120, new SmoothStepInterpolation());

string outputPath = Path.Combine(Path.GetTempPath(), "fractal_zoom.gif");
GifVideoWriter videoWriter = new(outputPath, frameRate: 24, repeat: true);

HeartbeatMessageMasterNode master = new(new TcpMessageNodeFactory(
    IPAddress.Loopback, 3000, new JsonSerializer()
).CreateMasterNode(), TimeSpan.FromSeconds(30));

MessageDispatcher dispatcher = new();
dispatcher.Register(new JoinMessageHandler(master));
dispatcher.Register(new HeartbeatMessageHandler(master));
dispatcher.Register(new RenderResultHandler(async result =>
{
    await videoWriter.WriteFrameAsync(result);
    Console.WriteLine($"[MASTER] Frame written.");
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
            FractalColorizerType.CyclingHsv,
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

await videoWriter.DisposeAsync();
Console.WriteLine($"[MASTER] GIF saved: {outputPath}");

await master.DisposeAsync();
