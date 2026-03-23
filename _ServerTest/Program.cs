using System.Net;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Dispatching;
using DistributedFractals.Server.Handlers.Master;
using DistributedFractals.Server.Heartbeat;
using DistributedFractals.Server.Serialization;
using DistributedFractals.Server.Tcp;

HeartbeatMasterNode master = new(new TcpMessageNodeFactory(
    IPAddress.Loopback, 3000, new JsonSerializer()
).CreateMasterNode(), TimeSpan.FromSeconds(5));

MessageDispatcher dispatcher = new();
dispatcher.Register(new MasterJoinMessageHandler(master));
dispatcher.Register(new MasterHeartbeatMessageHandler(master));

master.MessageReceived += async message =>
{
    await dispatcher.DispatchAsync(message);
};

master.WorkerRegistered += worker =>
    Console.WriteLine($"[MASTER] Worker registered: {worker}");

master.WorkerUnregistered += worker =>
    Console.WriteLine($"[MASTER] Worker unregistered (heartbeat timeout): {worker}");

master.StartAsync();

Console.WriteLine("[MASTER] Server started...");
Console.ReadLine();

await master.DisposeAsync();
