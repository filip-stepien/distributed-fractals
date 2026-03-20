using System.Net;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Serialization;
using DistributedFractals.Server.Tcp;

IMessageNodeFactory factory = new TcpMessageNodeFactory(
    address: IPAddress.Loopback, 
    port: 3000,
    messageSerializer: new JsonSerializer()
);

IMessageMasterNode master = factory.CreateMaster();

master.MessageReceived += async message =>
{
    Console.WriteLine($"[MASTER] Od {message.Sender.DisplayName}: {message.Content}");

    await master.SendToWorker(message.Sender, new MasterNodeMessage(
        $"Odebrałem: {message.Content}"
    ));
};

await master.ConnectAsync();

Console.WriteLine("Master is running. Press Enter to stop.");
Console.ReadLine();

await master.DisposeAsync();
