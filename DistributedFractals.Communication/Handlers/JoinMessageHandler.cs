using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Handlers;

public class JoinMessageHandler(IMessageServer master) : IMessageHandler<JoinMessage>
{
    public Task HandleAsync(JoinMessage message)
    {
        master.RegisterClient(new ClientIdentifier(message.Sender, message.DisplayName));
        return Task.CompletedTask;
    }
}
