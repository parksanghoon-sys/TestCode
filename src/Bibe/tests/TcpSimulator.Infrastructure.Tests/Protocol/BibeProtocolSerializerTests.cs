using FluentAssertions;
using System.Buffers.Binary;
using TcpSimulator.Domain.Entities;
using TcpSimulator.Domain.ValueObjects;
using TcpSimulator.Infrastructure.Protocol;

namespace TcpSimulator.Infrastructure.Tests.Protocol;

public class BibeProtocolSerializerTests
{
    private readonly BibeProtocolSerializer _sut = new();

    [Fact]
    public void Serialize_TextMessage_WritesCorrectFormat()
    {
        // Arrange
        var message = new TextMessage("Hello");
        Span<byte> buffer = stackalloc byte[1024];

        // Act
        int written = _sut.Serialize(message, buffer);

        // Assert
        written.Should().BeGreaterThan(0);

        // Magic number 검증 (0xAA 0x55 -> 0x55AA little-endian)
        ushort magic = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
        magic.Should().Be(ProtocolHeader.MagicNumber);

        // Length 검증
        int length = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(2));
        length.Should().Be(5); // "Hello".Length

        // Type 검증
        buffer[6].Should().Be(MessageType.Text);
    }

    [Fact]
    public void Deserialize_ValidBuffer_ReturnsMessage()
    {
        // Arrange
        var originalMessage = new TextMessage("Hello");
        Span<byte> buffer = stackalloc byte[1024];
        int written = _sut.Serialize(originalMessage, buffer);

        // Act
        var restoredMessage = _sut.Deserialize(buffer.Slice(0, written));

        // Assert
        restoredMessage.Should().NotBeNull();
        restoredMessage!.Type.Should().Be(MessageType.Text);
        restoredMessage.Payload.ToArray().Should().Equal(originalMessage.Payload.ToArray());
    }

    [Fact]
    public void Serialize_Then_Deserialize_RestoresMessage()
    {
        // Arrange
        var originalMessage = new TextMessage("Hello World!");
        Span<byte> buffer = stackalloc byte[1024];

        // Act
        int written = _sut.Serialize(originalMessage, buffer);
        var restoredMessage = _sut.Deserialize(buffer.Slice(0, written)) as TextMessage;

        // Assert
        restoredMessage.Should().NotBeNull();
        restoredMessage!.Text.Should().Be("Hello World!");
        restoredMessage.Type.Should().Be(originalMessage.Type);
    }

    [Fact]
    public void Deserialize_CorruptedCrc_ReturnsNull()
    {
        // Arrange
        var message = new TextMessage("Hello");
        Span<byte> buffer = stackalloc byte[1024];
        int written = _sut.Serialize(message, buffer);

        // CRC 손상 (마지막 바이트 변경)
        buffer[written - 1] ^= 0xFF;

        // Act
        var result = _sut.Deserialize(buffer.Slice(0, written));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_InvalidMagicNumber_ReturnsNull()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[1024];
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, 0x0000); // Invalid magic

        // Act
        var result = _sut.Deserialize(buffer);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void IsCompleteMessage_PartialBuffer_ReturnsFalse()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[5]; // 최소 길이(11)보다 짧음

        // Act
        bool isComplete = _sut.IsCompleteMessage(buffer, out int messageLength);

        // Assert
        isComplete.Should().BeFalse();
        messageLength.Should().Be(0);
    }

    [Fact]
    public void IsCompleteMessage_CompleteMessage_ReturnsTrue()
    {
        // Arrange
        var message = new TextMessage("Hello");
        Span<byte> buffer = stackalloc byte[1024];
        int written = _sut.Serialize(message, buffer);

        // Act
        bool isComplete = _sut.IsCompleteMessage(buffer.Slice(0, written), out int messageLength);

        // Assert
        isComplete.Should().BeTrue();
        messageLength.Should().Be(written);
    }

    [Fact]
    public void Serialize_BufferTooSmall_ReturnsMinusOne()
    {
        // Arrange
        var message = new TextMessage("Hello World This is a long message");
        Span<byte> buffer = stackalloc byte[5]; // Too small

        // Act
        int result = _sut.Serialize(message, buffer);

        // Assert
        result.Should().Be(-1);
    }

    [Fact]
    public void Serialize_EchoMessage_WritesCorrectType()
    {
        // Arrange
        var message = new EchoMessage("Echo"u8.ToArray());
        Span<byte> buffer = stackalloc byte[1024];

        // Act
        int written = _sut.Serialize(message, buffer);

        // Assert
        written.Should().BeGreaterThan(0);
        buffer[6].Should().Be(MessageType.Echo);
    }

    [Fact]
    public void Serialize_HeartbeatMessage_WritesCorrectType()
    {
        // Arrange
        var message = new HeartbeatMessage(12345L);
        Span<byte> buffer = stackalloc byte[1024];

        // Act
        int written = _sut.Serialize(message, buffer);

        // Assert
        written.Should().BeGreaterThan(0);
        buffer[6].Should().Be(MessageType.Heartbeat);
    }
}
