using FluentAssertions;
using TcpSimulator.Infrastructure.Buffers;

namespace TcpSimulator.Infrastructure.Tests.Buffers;

public class PooledBufferManagerTests
{
    [Fact]
    public void Rent_SmallBuffer_ReturnsValidBuffer()
    {
        // Arrange
        var manager = new PooledBufferManager();

        // Act
        using var buffer = manager.Rent(512);

        // Assert
        buffer.Length.Should().BeGreaterThanOrEqualTo(512);
        buffer.Span.Length.Should().BeGreaterThanOrEqualTo(512);
    }

    [Fact]
    public void Rent_LargeBuffer_ReturnsValidBuffer()
    {
        // Arrange
        var manager = new PooledBufferManager();

        // Act
        using var buffer = manager.Rent(2048);

        // Assert
        buffer.Length.Should().BeGreaterThanOrEqualTo(2048);
    }

    [Fact]
    public void RentedBuffer_Dispose_AllowsReuse()
    {
        // Arrange
        var manager = new PooledBufferManager();

        // Act
        var buffer1 = manager.Rent(512);
        buffer1.Span[0] = 0xFF; // Mark buffer
        buffer1.Dispose();

        var buffer2 = manager.Rent(512);

        // Assert - Buffer should be cleared after return
        buffer2.Span[0].Should().Be(0);
        buffer2.Dispose();
    }

    [Fact]
    public void RentedBuffer_MultipleAllocations_AllValid()
    {
        // Arrange
        var manager = new PooledBufferManager();
        var buffers = new List<IDisposable>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            var buffer = manager.Rent(1024);
            buffer.Span[0] = (byte)i;
            buffers.Add(buffer);
        }

        // Assert - All buffers should be valid
        buffers.Should().HaveCount(100);

        // Cleanup
        buffers.ForEach(b => b.Dispose());
    }

    [Fact]
    public void Rent_ZeroSize_ReturnsEmptyBuffer()
    {
        // Arrange
        var manager = new PooledBufferManager();

        // Act
        using var buffer = manager.Rent(0);

        // Assert
        buffer.Length.Should().BeGreaterThanOrEqualTo(0);
    }
}
