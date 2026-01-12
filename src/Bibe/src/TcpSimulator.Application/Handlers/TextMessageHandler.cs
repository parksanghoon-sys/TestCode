using TcpSimulator.Domain.Entities;
using TcpSimulator.Domain.Interfaces;
using TcpSimulator.Domain.ValueObjects;

namespace TcpSimulator.Application.Handlers;

public sealed class TextMessageHandler : IMessageHandler<TextMessage>
{
    public Task HandleAsync(TextMessage message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[Text] Received: {message.Text}");
        return Task.CompletedTask;
    }

    public bool CanHandle(byte messageType) => messageType == MessageType.Text;
}
