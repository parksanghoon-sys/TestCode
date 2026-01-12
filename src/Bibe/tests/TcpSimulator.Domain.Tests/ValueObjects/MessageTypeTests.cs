using FluentAssertions;
using TcpSimulator.Domain.ValueObjects;

namespace TcpSimulator.Domain.Tests.ValueObjects;

public class MessageTypeTests
{
    [Theory]
    [InlineData(MessageType.Echo)]
    [InlineData(MessageType.Text)]
    [InlineData(MessageType.Heartbeat)]
    [InlineData(MessageType.Custom)]
    public void MessageType_ValidType_CreatesSuccessfully(byte validType)
    {
        // Act
        var messageType = new MessageType(validType);

        // Assert
        messageType.Value.Should().Be(validType);
    }

    [Fact]
    public void MessageType_InvalidType_ThrowsArgumentException()
    {
        // Arrange
        byte invalidType = 0xAB;

        // Act
        Action act = () => new MessageType(invalidType);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid message type*");
    }

    [Fact]
    public void IsValid_ValidType_ReturnsTrue()
    {
        // Arrange & Act
        bool result = MessageType.IsValid(MessageType.Echo);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_InvalidType_ReturnsFalse()
    {
        // Arrange & Act
        bool result = MessageType.IsValid(0xAB);

        // Assert
        result.Should().BeFalse();
    }
}
