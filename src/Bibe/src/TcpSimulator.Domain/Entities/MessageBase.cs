using TcpSimulator.Domain.Interfaces;

namespace TcpSimulator.Domain.Entities;

public abstract class MessageBase : IMessage
{
    public byte Type { get; protected init; }
    public ReadOnlyMemory<byte> Payload { get; protected init; }
    public DateTime Timestamp { get; protected init; }

    protected MessageBase(byte type, ReadOnlyMemory<byte> payload)
    {
        Type = type;
        Payload = payload;
        Timestamp = DateTime.UtcNow;
    }
}
