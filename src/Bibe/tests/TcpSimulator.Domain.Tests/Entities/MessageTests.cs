using FluentAssertions;
using System.Text;
using TcpSimulator.Domain.Entities;
using TcpSimulator.Domain.ValueObjects;

namespace TcpSimulator.Domain.Tests.Entities;

public class MessageTests
{
    [Fact]
    public void TextMessage_FromString_CreatesWithCorrectProperties()
    {
        // Arrange
        string text = "Hello World";

        // Act
        var message = new TextMessage(text);

        // Assert
        message.Type.Should().Be(MessageType.Text);
        message.Text.Should().Be(text);
        message.Payload.Length.Should().Be(Encoding.UTF8.GetByteCount(text));
        message.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void TextMessage_FromPayload_CreatesWithCorrectText()
    {
        // Arrange
        string text = "Hello World";
        var payload = Encoding.UTF8.GetBytes(text);

        // Act
        var message = new TextMessage(payload);

        // Assert
        message.Type.Should().Be(MessageType.Text);
        message.Text.Should().Be(text);
    }

    [Fact]
    public void EchoMessage_FromPayload_CreatesWithCorrectType()
    {
        // Arrange
        var payload = "Echo test"u8.ToArray();

        // Act
        var message = new EchoMessage(payload);

        // Assert
        message.Type.Should().Be(MessageType.Echo);
        message.Payload.ToArray().Should().Equal(payload);
    }

    [Fact]
    public void HeartbeatMessage_FromSequenceNumber_CreatesCorrectly()
    {
        // Arrange
        long sequenceNumber = 12345L;

        // Act
        var message = new HeartbeatMessage(sequenceNumber);

        // Assert
        message.Type.Should().Be(MessageType.Heartbeat);
        message.SequenceNumber.Should().Be(sequenceNumber);
        message.Payload.Length.Should().Be(sizeof(long));
    }

    [Fact]
    public void HeartbeatMessage_FromPayload_ParsesSequenceNumber()
    {
        // Arrange
        long sequenceNumber = 12345L;
        var payload = BitConverter.GetBytes(sequenceNumber);

        // Act
        var message = new HeartbeatMessage(payload);

        // Assert
        message.SequenceNumber.Should().Be(sequenceNumber);
    }
}
