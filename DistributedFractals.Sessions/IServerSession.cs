using DistributedFractals.Orchestration.Schedulers;
using DistributedFractals.Server.Core;

namespace DistributedFractals.Sessions;

public interface IServerSession : IAsyncDisposable
{
    event Action<ClientIdentifier>? ClientConnected;
    event Action<ClientIdentifier>? ClientDisconnected;
    event Action<ClientIdentifier, int>? FrameDispatched;
    event Action<ClientIdentifier, int, TimeSpan>? FrameCompleted;
    event Action<ClientIdentifier, int>? FrameFailed;
    event Action<string>? TimingReportReady;
    event Action<RenderTimingSummary>? TimingSummaryReady;
    event Action? RenderCompleted;
    event Action<Exception>? RenderFailed;

    IReadOnlyCollection<ClientIdentifier> Clients { get; }

    Task StartAsync(ServerConnectionSettings connectionSettings);
    Task StartRenderAsync(RenderSettings renderSettings);
    void CancelRender();
    string? GetClientAddress(ClientIdentifier client);
}
