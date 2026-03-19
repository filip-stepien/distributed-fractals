using System.Net;
using DistributedFractals.Server.Core;

namespace DistributedFractals.Server.Tcp;

public class TcpMessageNodeFactory(IPAddress address, int port) : IMessageNodeFactory
{
    public IMessageMasterNode CreateMaster()
    {
        return new TcpServerNode(address, port);
    }

    public IMessageWorkerNode CreateWorker()
    {
        return new TcpClientNode(address, port);
    }
}