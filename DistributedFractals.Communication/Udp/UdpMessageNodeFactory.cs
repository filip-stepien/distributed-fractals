using System.Net;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Serialization;

namespace DistributedFractals.Server.Udp;

public class UdpMessageNodeFactory(IPAddress address, int port, ISerializer serializer) : IMessageNodeFactory
{
    public IMessageMasterNode CreateMasterNode() => new UdpServerNode(address, port, serializer);

    public IMessageWorkerNode CreateWorkerNode() => new UdpClientNode(address, port, serializer);
}
