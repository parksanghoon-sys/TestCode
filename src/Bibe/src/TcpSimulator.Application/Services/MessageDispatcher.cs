using TcpSimulator.Domain.Interfaces;

namespace TcpSimulator.Application.Services;

public sealed class MessageDispatcher : IMessageDispatcher
{
    private readonly Dictionary<byte, List<object>> _handlers = new();

    public void RegisterHandler<TMessage>(IMessageHandler<TMessage> handler)
        where TMessage : IMessage
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        byte messageType = GetMessageType<TMessage>();

        if (!_handlers.TryGetValue(messageType, out var list))
        {
            list = new List<object>();
            _handlers[messageType] = list;
        }

        list.Add(handler);
    }

    public async Task DispatchAsync(IMessage message, CancellationToken cancellationToken)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        if (!_handlers.TryGetValue(message.Type, out var list))
        {
            Console.WriteLine($"[Dispatcher] No handler for message type: {message.Type:X2}");
            return;
        }

        foreach (var handler in list)
        {
            try
            {
                await InvokeHandler(handler, message, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Dispatcher] Handler error: {ex.Message}");
            }
        }
    }

    private static async Task InvokeHandler(
        object handler,
        IMessage message,
        CancellationToken cancellationToken)
    {
        var handlerType = handler.GetType();
        var handleMethod = handlerType.GetMethod("HandleAsync");

        if (handleMethod != null)
        {
            var task = (Task)handleMethod.Invoke(
                handler,
                new object[] { message, cancellationToken })!;
            await task;
        }
    }

    private static byte GetMessageType<TMessage>() where TMessage : IMessage
    {
        var typeName = typeof(TMessage).Name;
        return typeName switch
        {
            "EchoMessage" => 0x01,
            "TextMessage" => 0x02,
            "HeartbeatMessage" => 0x03,
            _ => 0xFF
        };
    }
}
