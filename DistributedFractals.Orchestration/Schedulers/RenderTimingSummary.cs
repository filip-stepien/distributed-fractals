using DistributedFractals.Server.Core;

namespace DistributedFractals.Orchestration.Schedulers;

public sealed record RenderTimingSummary(
    TimeSpan RenderElapsed,
    int FrameCount,
    int BatchCount,
    int ClientCount,
    int FramesPerBatch,
    TimeSpan AverageFrameRender,
    TimeSpan AverageBatchRoundtrip,
    TimeSpan AverageBatchRender,
    TimeSpan AverageBatchCommunication,
    IReadOnlyList<ClientTimingSummary> Clients);

public sealed record ClientTimingSummary(
    ClientIdentifier Client,
    int FrameCount,
    int BatchCount,
    TimeSpan AverageFrameRender,
    TimeSpan AverageBatchRoundtrip,
    TimeSpan AverageBatchRender,
    TimeSpan AverageBatchCommunication);
