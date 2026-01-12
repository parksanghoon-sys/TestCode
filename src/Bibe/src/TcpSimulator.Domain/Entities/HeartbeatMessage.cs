using TcpSimulator.Domain.ValueObjects;

namespace TcpSimulator.Domain.Entities;

public sealed class HeartbeatMessage : MessageBase
{
    public long SequenceNumber { get; }

    public HeartbeatMessage(long sequenceNumber)
        : base(MessageType.Heartbeat, BitConverter.GetBytes(sequenceNumber))
    {
        SequenceNumber = sequenceNumber;
    }

    public HeartbeatMessage(ReadOnlyMemory<byte> payload)
        : base(MessageType.Heartbeat, payload)
    {
        SequenceNumber = BitConverter.ToInt64(payload.Span);
    }
}
