using DistributedFractals.Server.Core;

namespace DistributedFractals.Orchestration.Schedulers;

public record FrameTiming(
    ClientIdentifier Client,
    int FrameIndex,
    TimeSpan Roundtrip,
    TimeSpan RenderDuration
)
{
    public TimeSpan CommunicationOverhead => Roundtrip - RenderDuration;
}
