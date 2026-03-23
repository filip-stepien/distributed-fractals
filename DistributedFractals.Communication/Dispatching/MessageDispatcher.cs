using DistributedFractals.Server.Handlers;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Dispatching;

public sealed class MessageDispatcher : IMessageDispatcher
{
    private readonly Dictionary<Type, List<Func<BaseMessage, Task>>> _handlers = new();

    public void Register<TMessage>(params IMessageHandler<TMessage>[] handlers) where TMessage : BaseMessage
    {
        if (!_handlers.TryGetValue(typeof(TMessage), out var list))
        {
            list = new List<Func<BaseMessage, Task>>();
            _handlers[typeof(TMessage)] = list;
        }

        foreach (var handler in handlers)
        {
            list.Add(message => handler.HandleAsync((TMessage)message));
        }
    }

    public async Task DispatchAsync(BaseMessage baseMessage)
    {
        if (!_handlers.TryGetValue(baseMessage.GetType(), out var handlers))
        {
            return;
        }

        foreach (var handler in handlers)
        {
            await handler(baseMessage);
        }
    }
}
