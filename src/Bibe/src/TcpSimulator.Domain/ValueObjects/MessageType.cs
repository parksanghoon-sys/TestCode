namespace TcpSimulator.Domain.ValueObjects;

public readonly record struct MessageType
{
    public const byte Echo = 0x01;
    public const byte Text = 0x02;
    public const byte Heartbeat = 0x03;
    public const byte Custom = 0xFF;

    public byte Value { get; init; }

    public MessageType(byte value)
    {
        if (!IsValid(value))
            throw new ArgumentException($"Invalid message type: {value:X2}", nameof(value));
        Value = value;
    }

    public static bool IsValid(byte value) => value is Echo or Text or Heartbeat or Custom;
}
