namespace TcpSimulator.Domain.Interfaces;

public interface IProtocolSerializer
{
    /// <summary>
    /// Deserializes a message from a buffer. Returns null if deserialization fails.
    /// </summary>
    IMessage? Deserialize(ReadOnlySpan<byte> buffer);

    /// <summary>
    /// Serializes a message to a buffer. Returns number of bytes written, or -1 on failure.
    /// </summary>
    int Serialize(IMessage message, Span<byte> buffer);

    /// <summary>
    /// Checks if the buffer contains a complete message.
    /// </summary>
    bool IsCompleteMessage(ReadOnlySpan<byte> buffer, out int messageLength);
}
