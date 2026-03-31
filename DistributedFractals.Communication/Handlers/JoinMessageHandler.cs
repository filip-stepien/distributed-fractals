using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Handlers;

public class JoinMessageHandler(IMessageServer master) : IMessageHandler<JoinMessage>
{
    public Task HandleAsync(JoinMessage message)
    {
        master.RegisterClient(message.Sender);
        return Task.CompletedTask;
    }
}
