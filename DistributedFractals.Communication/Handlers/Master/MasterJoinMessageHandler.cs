using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Handlers.Master;

public class MasterJoinMessageHandler(IMessageMasterNode master) : IMessageHandler<JoinMessage>
{
    public Task HandleAsync(JoinMessage message)
    {
        master.RegisterWorker(message.Sender);
        Console.WriteLine($"[MASTER] Worker joined: {message.Sender.DisplayName}");
        return Task.CompletedTask;
    }
}
