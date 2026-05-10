using DistributedFractals.Server.Core;

namespace DistributedFractals.Orchestration.Schedulers;

public record FrameTiming(
    ClientIdentifier Client,
    int FrameIndex,
    int BatchId,
    TimeSpan RenderDuration
);

public record BatchTiming(
    ClientIdentifier Client,
    int BatchId,
    int FirstFrameIndex,
    int LastFrameIndex,
    int FrameCount,
    TimeSpan Roundtrip,
    TimeSpan RenderDuration
)
{
    public TimeSpan CommunicationOverhead => Roundtrip - RenderDuration;
}
