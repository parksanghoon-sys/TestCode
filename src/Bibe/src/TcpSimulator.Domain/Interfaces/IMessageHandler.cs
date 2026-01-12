namespace TcpSimulator.Domain.Interfaces;

public interface IMessageHandler<in TMessage> where TMessage : IMessage
{
    Task HandleAsync(TMessage message, CancellationToken cancellationToken = default);
    bool CanHandle(byte messageType);
}
