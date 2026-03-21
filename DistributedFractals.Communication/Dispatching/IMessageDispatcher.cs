using DistributedFractals.Server.Handlers;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Dispatching;

public interface IMessageDispatcher
{
    void Register<TMessage>(params IMessageHandler<TMessage>[] handlers) where TMessage : Message;

    Task DispatchAsync(Message message);
}
