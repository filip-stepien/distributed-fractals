using System.Net;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Serialization;

namespace DistributedFractals.Server.Tcp;

public class TcpMessageNodeFactory(IPAddress address, int port, ISerializer serializer) : IMessageNodeFactory
{
    public IMessageMasterNode CreateMasterNode() => new TcpServerNode(address, port, serializer);

    public IMessageWorkerNode CreateWorkerNode() => new TcpClientNode(address, port, serializer);
}
