namespace TcpSimulator.Domain.Interfaces;

public interface IMessage
{
    byte Type { get; }
    ReadOnlyMemory<byte> Payload { get; }
    DateTime Timestamp { get; }
}
