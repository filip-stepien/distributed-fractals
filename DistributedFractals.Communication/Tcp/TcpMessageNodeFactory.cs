using System.Net;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Serialization;

namespace DistributedFractals.Server.Tcp;

public class TcpMessageNodeFactory(IPAddress address, int port, ISerializer messageSerializer) : IMessageNodeFactory
{
    public IMessageMasterNode CreateMaster()
    {
        return new TcpServerNode(address, port, messageSerializer);
    }

    public IMessageWorkerNode CreateWorker()
    {
        return new TcpClientNode(address, port, messageSerializer);
    }
}