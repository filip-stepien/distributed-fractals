namespace DistributedFractals.Server.Core;

public interface ITransportFactory
{
    IMessageServer CreateServer();
    IMessageClient CreateClient();
}