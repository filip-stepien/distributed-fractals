namespace DistributedFractals.Sessions;

public interface IServerSession : IAsyncDisposable
{
    event Action<Guid, string>? ClientConnected;
    event Action<Guid>? ClientDisconnected;
    event Action<Guid, int>? FrameDispatched;
    event Action<Guid, int, TimeSpan>? FrameCompleted;
    event Action<Guid, int>? FrameFailed;
    event Action? RenderCompleted;

    Task StartAsync(ConnectionSettings conn);
    Task StartRenderAsync(RenderSettings settings);
    void CancelRender();
}
