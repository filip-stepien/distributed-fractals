using System.Net;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Serialization;
using DistributedFractals.Server.Tcp;
using DistributedFractals.Server.Udp;

namespace DistributedFractals.Sessions;

internal static class TransportFactoryResolver
{
    public static ITransportFactory FromConnectionSettings(ConnectionSettings conn)
    {
        IPAddress address = IPAddress.Parse(conn.Address);
        JsonSerializer serializer = new();

        return conn.Protocol switch
        {
            TransportProtocol.Tcp => new TcpTransportFactory(address, conn.Port, serializer),
            TransportProtocol.Udp => new UdpTransportFactory(address, conn.Port, serializer),
            _ => throw new NotSupportedException($"Unknown protocol: {conn.Protocol}")
        };
    }
}
