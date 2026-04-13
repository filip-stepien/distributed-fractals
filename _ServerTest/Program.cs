using System.Net;
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
    IPAddress.Loopback, 3000, new JsonSerializer()
).CreateServer(), TimeSpan.FromSeconds(30));

List<RenderFrameMessage> frames = frameBounds
    .Select((bounds, i) => new RenderFrameMessage(
        master.Identifier,
        i,
        FractalColorizerType.CyclingHsv,
        baseOptions,
        bounds))
    .ToList();

FrameScheduler scheduler = new(master, frames, new RoundRobinClientSelector(), framesPerClient: 1);

MessageDispatcher dispatcher = new();
dispatcher.Register(new JoinMessageHandler(master));
dispatcher.Register(new HeartbeatMessageHandler(master));
dispatcher.Register(new RenderResultHandler(scheduler));

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
Logger.Log("All frames received. Saving GIF...");

string outputPath = Path.Combine(Path.GetTempPath(), "fractal_zoom.gif");
GifVideoWriter videoWriter = new(outputPath, frameRate: 24, repeat: true);

foreach (FractalResult frame in scheduler.GetOrderedResults())
{
    await videoWriter.WriteFrameAsync(frame);
}

await videoWriter.DisposeAsync();
Logger.Log($"GIF saved: {outputPath}");

await master.DisposeAsync();
