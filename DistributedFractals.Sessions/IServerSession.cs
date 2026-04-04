using DistributedFractals.Server.Core;

namespace DistributedFractals.Sessions;

public interface IServerSession : IAsyncDisposable
{
    event Action<ClientIdentifier>? ClientConnected;
    event Action<ClientIdentifier>? ClientDisconnected;
    event Action<ClientIdentifier, int>? FrameDispatched;
    event Action<ClientIdentifier, int, TimeSpan>? FrameCompleted;
    event Action<ClientIdentifier, int>? FrameFailed;
    event Action? RenderCompleted;

    Task StartAsync(ConnectionSettings connectionSettings);
    Task StartRenderAsync(RenderSettings renderSettings);
    void CancelRender();
}
