using BenchmarkDotNet.Attributes;
using TcpSimulator.Domain.Interfaces;
using TcpSimulator.Infrastructure.Buffers;

namespace TcpSimulator.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class BufferManagementBenchmarks
{
    private readonly IBufferManager _bufferManager = new PooledBufferManager();

    [Benchmark]
    public void RentAndReturn_SmallBuffer_1KB()
    {
        using var buffer = _bufferManager.Rent(1024);
        // Simulate usage
        buffer.Span[0] = 0xFF;
    }

    [Benchmark]
    public void RentAndReturn_MediumBuffer_8KB()
    {
        using var buffer = _bufferManager.Rent(8192);
        buffer.Span[0] = 0xFF;
    }

    [Benchmark]
    public void RentAndReturn_LargeBuffer_64KB()
    {
        using var buffer = _bufferManager.Rent(65536);
        buffer.Span[0] = 0xFF;
    }

    [Benchmark]
    public void MultipleRentReturn_Sequential()
    {
        for (int i = 0; i < 10; i++)
        {
            using var buffer = _bufferManager.Rent(4096);
            buffer.Span[0] = (byte)i;
        }
    }

    [Benchmark]
    public void BufferPooling_ReuseTest()
    {
        // Test buffer reuse efficiency
        var buffers = new RentedBuffer<byte>[5];

        // Rent
        for (int i = 0; i < buffers.Length; i++)
        {
            buffers[i] = _bufferManager.Rent(2048);
        }

        // Return
        for (int i = 0; i < buffers.Length; i++)
        {
            buffers[i].Dispose();
        }

        // Re-rent (should reuse)
        for (int i = 0; i < buffers.Length; i++)
        {
            buffers[i] = _bufferManager.Rent(2048);
            buffers[i].Dispose();
        }
    }
}
