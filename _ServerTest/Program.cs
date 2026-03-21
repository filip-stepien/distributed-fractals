using System.Net;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Dispatching;
using DistributedFractals.Server.Handlers;
using DistributedFractals.Server.Serialization;
using DistributedFractals.Server.Tcp;

IMessageNodeFactory factory = new TcpMessageNodeFactory(
    address: IPAddress.Loopback,
    port: 3000,
    messageSerializer: new JsonSerializer()
);

IMessageMasterNode master = factory.CreateMaster();

IMessageDispatcher dispatcher = new MessageDispatcher();
dispatcher.Register(new HeartbeatMessageHandler());
dispatcher.Register(new TextMessageHandler());

master.MessageReceived += async message =>
{
    await dispatcher.DispatchAsync(message);
};

await master.StartAsync();

Console.WriteLine("[MASTER] Server started...");
Console.ReadLine();

await master.DisposeAsync();