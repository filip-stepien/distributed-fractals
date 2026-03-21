using System.Net;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Dispatching;
using DistributedFractals.Server.Handlers;
using DistributedFractals.Server.Messages;
using DistributedFractals.Server.Serialization;
using DistributedFractals.Server.Tcp;

IMessageNodeFactory factory = new TcpMessageNodeFactory(
    address: IPAddress.Loopback,
    port: 3000,
    messageSerializer: new JsonSerializer()
);

await using IMessageWorkerNode worker = factory.CreateWorker();
worker.Identifier.DisplayName = "worker-1";

IMessageDispatcher dispatcher = new MessageDispatcher();
dispatcher.Register(new TextMessageHandler());

worker.MessageReceived += async message =>
{
    await dispatcher.DispatchAsync(message);
};

await worker.StartAsync();

await worker.SendToMaster(new HeartbeatMessage(worker.Identifier));
await Task.Delay(500);
await worker.SendToMaster(new TextMessage(worker.Identifier, "hi master"));

Console.WriteLine("[WORKER] Done.");
Console.ReadLine();