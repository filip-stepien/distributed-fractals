using System.Net;
using System.Runtime;
using DistributedFractals.Fractal.Colorizers;
using DistributedFractals.Fractal.Core;
using DistributedFractals.Fractal.Generators.Mandelbrot;
using DistributedFractals.Fractal.Mandelbrot;
using DistributedFractals.Fractal.Zoom;
using DistributedFractals.Fractal.Zoom.Interpolations;
using DistributedFractals.Logging;
using DistributedFractals.Orchestration.Schedulers;
using DistributedFractals.Orchestration.Selectors;
using DistributedFractals.Server.Dispatchers;
using DistributedFractals.Server.Handlers;
using DistributedFractals.Server.Heartbeat;
using DistributedFractals.Server.Messages;
using DistributedFractals.Server.Serializers;
using DistributedFractals.Server.Tcp;
using DistributedFractals.Video.Gif;

Logger.Initialize(new ConsoleLogger());

MandelbrotOptions baseOptions = new(400, 300, MaxIterations: 500);

List<ZoomKeyframe> keyframes =
[
    new ZoomKeyframe(T: 0.0, CenterRe: -0.75,    CenterIm:  0.0,    Scale: 1.0),
    new ZoomKeyframe(T: 0.5, CenterRe: -0.7269,  CenterIm:  0.1889, Scale: 0.05),
    new ZoomKeyframe(T: 1.0, CenterRe: -0.74529, CenterIm:  0.1130, Scale: 0.001),
];

List<FrameBounds> frameBounds = new KeyframeZoomSequenceGenerator()
    .Generate(baseOptions, keyframes, totalFrames: 120, new SmoothStepInterpolation())
    .ToList();

HeartbeatMessageServer master = new(new TcpTransportFactory(
    IPAddress.Loopback, 3000, new BinaryMessageSerializer()
).CreateServer(), TimeSpan.FromSeconds(30));

Guid renderJobId = Guid.NewGuid();
List<RenderFrameMessage> frames = frameBounds
    .Select((bounds, i) => new RenderFrameMessage(
        master.Identifier,
        renderJobId,
        i,
        FractalColorizerType.CyclingHsv,
        baseOptions,
        bounds))
    .ToList();

FrameScheduler scheduler = new(master, frames, new RoundRobinClientSelector(), framesPerBatch: 1);

MessageDispatcher dispatcher = new();
dispatcher.Register(new JoinMessageHandler(master));
dispatcher.Register(new HeartbeatMessageHandler(master));

RenderResultHandler renderResultHandler = new(scheduler);
dispatcher.Register<RenderResultMessage>(renderResultHandler);
dispatcher.Register<RenderBatchResultMessage>(renderResultHandler);

master.MessageReceived += async message =>
{
    await dispatcher.DispatchAsync(message);
};

master.ClientRegistered += client =>
{
    Logger.Log($"Worker joined: {client}");
    scheduler.OnClientAvailable(client);
};

master.ClientUnregistered += client =>
{
    Logger.Log($"Worker unregistered: {client}.");
    scheduler.OnClientFailed(client);
};

await master.StartAsync();
Logger.Log("Server started. Waiting for workers...");

await scheduler.WaitForAllAsync();
Logger.Log($"Render wall-clock time: {scheduler.RenderElapsed.TotalSeconds:F3}s");
Logger.Log(scheduler.GetTimingReport());
Logger.Log("All frames received. Saving GIF...");

string outputPath = Path.Combine(Path.GetTempPath(), "fractal_zoom.gif");
GifVideoWriter videoWriter = new(outputPath, frameRate: 24, repeat: true);

List<FractalResult?> orderedFrames = scheduler.DrainOrderedResults()
    .Cast<FractalResult?>()
    .ToList();

for (int i = 0; i < orderedFrames.Count; i++)
{
    FractalResult? frame = orderedFrames[i];
    if (frame is null)
    {
        continue;
    }

    await videoWriter.WriteFrameAsync(frame);
    orderedFrames[i] = null;
}

orderedFrames.Clear();
GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
GC.WaitForPendingFinalizers();
await videoWriter.DisposeAsync();
Logger.Log($"GIF saved: {outputPath}");

await master.DisposeAsync();
