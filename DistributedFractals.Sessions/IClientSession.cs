using DistributedFractals.Fractal.Core;
using DistributedFractals.Server.Core;

namespace DistributedFractals.Sessions;

public interface IClientSession : IAsyncDisposable
{
    event Action? Connected;
    event Action? Disconnected;
    event Action<int>? FrameStarted;
    event Action<int, TimeSpan, FractalResult>? FrameCompleted;
    event Action<int>? FrameFailed;

    Task ConnectAsync(string displayName, ConnectionSettings connectionSettings);
}
