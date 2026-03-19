namespace DistributedFractals.Server.Core;

public interface IMessageNode : IAsyncDisposable
{
    Task ConnectAsync();
}