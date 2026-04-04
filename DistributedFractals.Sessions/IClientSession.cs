using DistributedFractals.Core.Core;

namespace DistributedFractals.Sessions;

public interface IClientSession : IAsyncDisposable
{
    event Action? Connected;
    event Action? Disconnected;
    event Action<int>? FrameStarted;
    event Action<int, TimeSpan, FractalResult>? FrameCompleted;
    event Action<int>? FrameFailed;

    Task ConnectAsync(ConnectionSettings conn);
}
