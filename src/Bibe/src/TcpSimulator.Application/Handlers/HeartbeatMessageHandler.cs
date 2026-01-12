using TcpSimulator.Domain.Entities;
using TcpSimulator.Domain.Interfaces;
using TcpSimulator.Domain.ValueObjects;

namespace TcpSimulator.Application.Handlers;

public sealed class HeartbeatMessageHandler : IMessageHandler<HeartbeatMessage>
{
    public Task HandleAsync(HeartbeatMessage message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[Heartbeat] Sequence: {message.SequenceNumber}");
        return Task.CompletedTask;
    }

    public bool CanHandle(byte messageType) => messageType == MessageType.Heartbeat;
}
