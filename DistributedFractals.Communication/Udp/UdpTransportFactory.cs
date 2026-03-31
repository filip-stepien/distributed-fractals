using System.Net;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Serialization;

namespace DistributedFractals.Server.Udp;

public class UdpTransportFactory(IPAddress address, int port, ISerializer serializer) : ITransportFactory
{
    public IMessageServer CreateServer() => new UdpServer(address, port, serializer);

    public IMessageClient CreateClient() => new UdpClient(address, port, serializer);
}
