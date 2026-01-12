namespace TcpSimulator.Domain.Interfaces;

public interface IMessageDispatcher
{
    void RegisterHandler<TMessage>(IMessageHandler<TMessage> handler) where TMessage : IMessage;
    Task DispatchAsync(IMessage message, CancellationToken cancellationToken);
}
