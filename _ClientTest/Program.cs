using System.Net;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Tcp;

IMessageNodeFactory factory = new TcpMessageNodeFactory(IPAddress.Loopback, 3000);
await using IMessageNode worker = factory.CreateWorker();

worker.Identifier.DisplayName = "worker";

worker.MessageReceived += message =>
{
    Console.WriteLine($"[WORKER] Od {message.Sender.DisplayName}: {message.Content}");
};

await worker.ConnectAsync();

await worker.SendAsync(new Message(
    worker.Identifier,
    MessageNodeIdentifier.Master,
    "Hello World!"
));

Console.WriteLine("Press Enter to stop.");
Console.ReadLine();