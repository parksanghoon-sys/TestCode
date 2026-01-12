using TcpSimulator.Domain.Entities;
using TcpSimulator.Domain.Interfaces;
using TcpSimulator.Domain.ValueObjects;

namespace TcpSimulator.Application.Handlers;

public sealed class EchoMessageHandler : IMessageHandler<EchoMessage>
{
    private readonly Func<ValueTask> _sendCallback;

    public EchoMessageHandler(Func<ValueTask> sendCallback)
    {
        _sendCallback = sendCallback ?? throw new ArgumentNullException(nameof(sendCallback));
    }

    public async Task HandleAsync(EchoMessage message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[Echo] Received: {message.Payload.Length} bytes");

        // Echo back via callback
        await _sendCallback();
    }

    public bool CanHandle(byte messageType) => messageType == MessageType.Echo;
}
