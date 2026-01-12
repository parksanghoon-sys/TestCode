using FluentAssertions;
using TcpSimulator.Domain.Entities;
using TcpSimulator.Domain.ValueObjects;
using TcpSimulator.Infrastructure.Buffers;
using TcpSimulator.Infrastructure.Protocol;

namespace TcpSimulator.Infrastructure.Tests.Buffers;

public class StreamBufferAssemblerTests
{
    [Fact]
    public void TryAssembleMessage_PartialData_ReturnsNull()
    {
        // Arrange
        var serializer = new BibeProtocolSerializer();
        var bufferManager = new PooledBufferManager();
        using var assembler = new StreamBufferAssembler(serializer, bufferManager);

        Span<byte> partialData = stackalloc byte[5]; // Incomplete message

        // Act
        var message = assembler.TryAssembleMessage(partialData);

        // Assert
        message.Should().BeNull();
    }

    [Fact]
    public void TryAssembleMessage_CompleteMessage_ReturnsMessage()
    {
        // Arrange
        var serializer = new BibeProtocolSerializer();
        var bufferManager = new PooledBufferManager();
        using var assembler = new StreamBufferAssembler(serializer, bufferManager);

        var originalMessage = new TextMessage("Hello");
        Span<byte> buffer = stackalloc byte[1024];
        int written = serializer.Serialize(originalMessage, buffer);

        // Act
        var message = assembler.TryAssembleMessage(buffer.Slice(0, written));

        // Assert
        message.Should().NotBeNull();
        message!.Type.Should().Be(MessageType.Text);
    }

    [Fact]
    public void TryAssembleMessage_MultipleMessages_AssemblesAll()
    {
        // Arrange
        var serializer = new BibeProtocolSerializer();
        var bufferManager = new PooledBufferManager();
        using var assembler = new StreamBufferAssembler(serializer, bufferManager);

        var msg1 = new TextMessage("Hello");
        var msg2 = new TextMessage("World");

        Span<byte> buffer = stackalloc byte[2048];
        int written1 = serializer.Serialize(msg1, buffer);
        int written2 = serializer.Serialize(msg2, buffer.Slice(written1));

        // Act & Assert - First message
        var message1 = assembler.TryAssembleMessage(buffer.Slice(0, written1 + written2));
        message1.Should().NotBeNull();
        (message1 as TextMessage)!.Text.Should().Be("Hello");

        // Second message
        var message2 = assembler.TryAssembleMessage(ReadOnlySpan<byte>.Empty);
        message2.Should().NotBeNull();
        (message2 as TextMessage)!.Text.Should().Be("World");
    }

    [Fact]
    public void TryAssembleMessage_FragmentedMessage_AssemblesCorrectly()
    {
        // Arrange
        var serializer = new BibeProtocolSerializer();
        var bufferManager = new PooledBufferManager();
        using var assembler = new StreamBufferAssembler(serializer, bufferManager);

        var originalMessage = new TextMessage("Hello");
        Span<byte> buffer = stackalloc byte[1024];
        int written = serializer.Serialize(originalMessage, buffer);

        // Act - Send message in fragments
        var result1 = assembler.TryAssembleMessage(buffer.Slice(0, 5));
        result1.Should().BeNull(); // Incomplete

        var result2 = assembler.TryAssembleMessage(buffer.Slice(5, written - 5));
        result2.Should().NotBeNull(); // Complete
    }

    [Fact]
    public void Dispose_ReleasesResources()
    {
        // Arrange
        var serializer = new BibeProtocolSerializer();
        var bufferManager = new PooledBufferManager();
        var assembler = new StreamBufferAssembler(serializer, bufferManager);

        // Act & Assert - Should not throw
        assembler.Dispose();
    }
}
