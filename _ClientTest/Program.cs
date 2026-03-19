using System.Net;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Tcp;

IMessageNodeFactory factory = new TcpMessageNodeFactory(IPAddress.Loopback, 3000);
await using IMessageWorkerNode worker = factory.CreateWorker();

worker.Identifier.DisplayName = "worker-1";

worker.MessageReceived += message =>
{
    Console.WriteLine($"[WORKER] Od mastera: {message.Content}");
};

await worker.ConnectAsync();

await worker.SendToMaster(new WorkerNodeMessage(
    worker.Identifier,
    "Hello from worker!"
));

Console.WriteLine("Press Enter to stop.");
Console.ReadLine();

