using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Handlers;

public interface IMessageHandler<in TMessage>
    where TMessage : BaseMessage
{
    Task HandleAsync(TMessage message);
}
