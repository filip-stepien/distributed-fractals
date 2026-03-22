using System.Net;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Dispatching;
using DistributedFractals.Server.Serialization;
using DistributedFractals.Server.Tcp;

IMessageMasterNode master = new TcpMessageNodeFactory(
    IPAddress.Loopback, 3000, new JsonSerializer()
).CreateMasterNode();

IMessageDispatcher dispatcher = MessageDispatcherFactory.CreateMasterDispatcher(master);

master.MessageReceived += async message =>
{
    await dispatcher.DispatchAsync(message);
};

await master.StartAsync();

Console.WriteLine("[MASTER] Server started...");
Console.ReadLine();

await master.DisposeAsync();
