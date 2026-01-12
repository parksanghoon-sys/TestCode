using FluentAssertions;
using TcpSimulator.Domain.ValueObjects;

namespace TcpSimulator.Domain.Tests.ValueObjects;

public class ProtocolHeaderTests
{
    [Fact]
    public void ProtocolHeader_ValidHeader_IsValidReturnsTrue()
    {
        // Arrange
        var header = new ProtocolHeader
        {
            Magic = ProtocolHeader.MagicNumber,
            PayloadLength = 10,
            Type = MessageType.Echo
        };

        // Act & Assert
        header.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ProtocolHeader_InvalidMagic_IsValidReturnsFalse()
    {
        // Arrange
        var header = new ProtocolHeader
        {
            Magic = 0x0000,
            PayloadLength = 10,
            Type = MessageType.Echo
        };

        // Act & Assert
        header.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ProtocolHeader_NegativePayloadLength_IsValidReturnsFalse()
    {
        // Arrange
        var header = new ProtocolHeader
        {
            Magic = ProtocolHeader.MagicNumber,
            PayloadLength = -1,
            Type = MessageType.Echo
        };

        // Act & Assert
        header.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ProtocolHeader_Constants_HaveCorrectValues()
    {
        // Assert
        ProtocolHeader.MagicNumber.Should().Be(0x55AA);
        ProtocolHeader.HeaderSize.Should().Be(7); // 2 + 4 + 1
        ProtocolHeader.ChecksumSize.Should().Be(4);
        ProtocolHeader.MinMessageSize.Should().Be(11); // 7 + 4
    }
}
