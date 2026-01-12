using FluentAssertions;
using TcpSimulator.Infrastructure.Protocol;

namespace TcpSimulator.Infrastructure.Tests.Protocol;

public class Crc32CalculatorTests
{
    [Fact]
    public void Calculate_EmptyData_ReturnsZero()
    {
        // Arrange
        Span<byte> data = Span<byte>.Empty;

        // Act
        uint result = Crc32Calculator.Calculate(data);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void Calculate_SameData_ReturnsSameChecksum()
    {
        // Arrange
        Span<byte> data1 = "Hello World"u8.ToArray();
        Span<byte> data2 = "Hello World"u8.ToArray();

        // Act
        uint checksum1 = Crc32Calculator.Calculate(data1);
        uint checksum2 = Crc32Calculator.Calculate(data2);

        // Assert
        checksum1.Should().Be(checksum2);
    }

    [Fact]
    public void Calculate_DifferentData_ReturnsDifferentChecksum()
    {
        // Arrange
        Span<byte> data1 = "Hello World"u8.ToArray();
        Span<byte> data2 = "Hello Worldx"u8.ToArray();

        // Act
        uint checksum1 = Crc32Calculator.Calculate(data1);
        uint checksum2 = Crc32Calculator.Calculate(data2);

        // Assert
        checksum1.Should().NotBe(checksum2);
    }

    [Fact]
    public void Calculate_KnownValue_ReturnsExpectedChecksum()
    {
        // Arrange
        // CRC32("123456789") should be 0xCBF43926
        Span<byte> data = "123456789"u8.ToArray();

        // Act
        uint checksum = Crc32Calculator.Calculate(data);

        // Assert
        checksum.Should().Be(0xCBF43926);
    }
}
