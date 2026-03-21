using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Handlers;

public interface IMessageHandler<in TMessage>
    where TMessage : Message
{
    Task HandleAsync(TMessage message);
}
