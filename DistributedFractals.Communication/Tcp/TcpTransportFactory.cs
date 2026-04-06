using System.Net;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Serializers;

namespace DistributedFractals.Server.Tcp;

public class TcpTransportFactory(IPAddress address, int port, ISerializer serializer) : ITransportFactory
{
    public IMessageServer CreateServer() => new TcpServer(address, port, serializer);

    public IMessageClient CreateClient() => new TcpClient(address, port, serializer);
}
