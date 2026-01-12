using System.Buffers.Binary;
using TcpSimulator.Domain.Entities;
using TcpSimulator.Domain.Interfaces;
using TcpSimulator.Domain.ValueObjects;

namespace TcpSimulator.Infrastructure.Protocol;

public sealed class BibeProtocolSerializer : IProtocolSerializer
{
    /// <summary>
    /// Protocol format: [Magic 2B][Length 4B][Type 1B][Payload ...][CRC32 4B]
    /// </summary>
    public IMessage? Deserialize(ReadOnlySpan<byte> buffer)
    {
        if (!IsCompleteMessage(buffer, out int messageLength))
            return null;

        // Magic number 검증
        ushort magic = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
        if (magic != ProtocolHeader.MagicNumber)
            return null;

        // Length 읽기
        int payloadLength = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(2));
        if (payloadLength < 0 || payloadLength > buffer.Length - ProtocolHeader.MinMessageSize)
            return null;

        // Type 읽기
        byte type = buffer[6];

        // Payload 추출
        var payloadSpan = buffer.Slice(ProtocolHeader.HeaderSize, payloadLength);

        // CRC32 검증
        int checksumOffset = ProtocolHeader.HeaderSize + payloadLength;
        uint expectedChecksum = BinaryPrimitives.ReadUInt32LittleEndian(
            buffer.Slice(checksumOffset));
        uint actualChecksum = Crc32Calculator.Calculate(
            buffer.Slice(0, checksumOffset));

        if (expectedChecksum != actualChecksum)
            return null;

        // Payload를 Memory로 복사
        var payloadMemory = payloadSpan.ToArray().AsMemory();

        // MessageFactory로 생성
        return CreateMessage(type, payloadMemory);
    }

    public int Serialize(IMessage message, Span<byte> buffer)
    {
        int payloadLength = message.Payload.Length;
        int totalLength = ProtocolHeader.MinMessageSize + payloadLength;

        if (buffer.Length < totalLength)
            return -1;

        // Magic number (little-endian)
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, ProtocolHeader.MagicNumber);

        // Payload length (little-endian)
        BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(2), payloadLength);

        // Type
        buffer[6] = message.Type;

        // Payload
        message.Payload.Span.CopyTo(buffer.Slice(ProtocolHeader.HeaderSize));

        // CRC32 계산 및 쓰기
        int checksumOffset = ProtocolHeader.HeaderSize + payloadLength;
        uint checksum = Crc32Calculator.Calculate(buffer.Slice(0, checksumOffset));
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(checksumOffset), checksum);

        return totalLength;
    }

    public bool IsCompleteMessage(ReadOnlySpan<byte> buffer, out int messageLength)
    {
        messageLength = 0;

        if (buffer.Length < ProtocolHeader.MinMessageSize)
            return false;

        // Magic number 체크
        ushort magic = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
        if (magic != ProtocolHeader.MagicNumber)
            return false;

        // Payload length 읽기
        int payloadLength = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(2));
        if (payloadLength < 0)
            return false;

        messageLength = ProtocolHeader.MinMessageSize + payloadLength;

        return buffer.Length >= messageLength;
    }

    private static IMessage? CreateMessage(byte type, ReadOnlyMemory<byte> payload)
    {
        return type switch
        {
            MessageType.Echo => new EchoMessage(payload),
            MessageType.Text => new TextMessage(payload),
            MessageType.Heartbeat => new HeartbeatMessage(payload),
            _ => null
        };
    }
}
