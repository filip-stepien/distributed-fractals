using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Handlers.Master;

public class MasterJoinMessageHandler(IMessageMasterNode master) : IMessageHandler<JoinBaseMessage>
{
    public Task HandleAsync(JoinBaseMessage baseMessage)
    {
        master.RegisterWorker(baseMessage.Sender);
        Console.WriteLine($"[MASTER] Worker joined: {baseMessage.Sender}");
        return Task.CompletedTask;
    }
}
