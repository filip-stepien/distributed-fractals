using DistributedFractals.Server.Handlers;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Dispatchers;

public interface IMessageDispatcher
{
    void Register<TMessage>(params IMessageHandler<TMessage>[] handlers) where TMessage : BaseMessage;

    Task DispatchAsync(BaseMessage baseMessage);
}