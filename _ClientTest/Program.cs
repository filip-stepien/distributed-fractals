using System.Net;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Dispatching;
using DistributedFractals.Server.Messages;
using DistributedFractals.Server.Serialization;
using DistributedFractals.Server.Tcp;

IMessageWorkerNode worker = new TcpMessageNodeFactory(
    IPAddress.Loopback, 3000, new JsonSerializer()
).CreateWorkerNode();

worker.Identifier.DisplayName = "worker-1";

IMessageDispatcher dispatcher = MessageDispatcherFactory.CreateWorkerDispatcher();

worker.MessageReceived += async message =>
{
    await dispatcher.DispatchAsync(message);
};

await worker.StartAsync();
await worker.SendToMasterAsync(new JoinMessage(worker.Identifier));

Console.WriteLine("[WORKER] Joined.");

await Task.Delay(500);
await worker.SendToMasterAsync(new HeartbeatMessage(worker.Identifier));
await worker.SendToMasterAsync(new TextMessage(worker.Identifier, "hi master"));

Console.WriteLine("[WORKER] Done.");
Console.ReadLine();

await worker.DisposeAsync();
