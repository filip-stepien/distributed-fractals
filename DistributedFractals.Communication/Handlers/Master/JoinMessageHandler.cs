using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Handlers.Master;

public class JoinMessageHandler(IMessageMasterNode master) : IMessageHandler<JoinMessage>
{
    public Task HandleAsync(JoinMessage message)
    {
        master.RegisterWorker(message.Sender);
        Console.WriteLine($"[MASTER] Worker joined: {message.Sender}");
        return Task.CompletedTask;
    }
}
