using TcpSimulator.Domain.ValueObjects;

namespace TcpSimulator.Domain.Entities;

public sealed class EchoMessage : MessageBase
{
    public EchoMessage(ReadOnlyMemory<byte> payload)
        : base(MessageType.Echo, payload)
    {
    }
}
