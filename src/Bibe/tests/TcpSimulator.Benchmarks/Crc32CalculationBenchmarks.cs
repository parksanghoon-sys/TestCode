using BenchmarkDotNet.Attributes;
using TcpSimulator.Infrastructure.Protocol;

namespace TcpSimulator.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class Crc32CalculationBenchmarks
{
    private readonly byte[] _smallData = new byte[64];
    private readonly byte[] _mediumData = new byte[1024];
    private readonly byte[] _largeData = new byte[8192];
    private readonly byte[] _veryLargeData = new byte[65536];

    [GlobalSetup]
    public void Setup()
    {
        // Fill with random data
        var random = new Random(42);
        random.NextBytes(_smallData);
        random.NextBytes(_mediumData);
        random.NextBytes(_largeData);
        random.NextBytes(_veryLargeData);
    }

    [Benchmark]
    public uint Calculate_64Bytes()
    {
        return Crc32Calculator.Calculate(_smallData);
    }

    [Benchmark]
    public uint Calculate_1KB()
    {
        return Crc32Calculator.Calculate(_mediumData);
    }

    [Benchmark]
    public uint Calculate_8KB()
    {
        return Crc32Calculator.Calculate(_largeData);
    }

    [Benchmark]
    public uint Calculate_64KB()
    {
        return Crc32Calculator.Calculate(_veryLargeData);
    }

    [Benchmark]
    public uint Calculate_Slice_Performance()
    {
        // Test slicing overhead
        var slice = _mediumData.AsSpan(100, 500);
        return Crc32Calculator.Calculate(slice);
    }

    [Benchmark]
    public void Multiple_Small_Calculations()
    {
        // Simulate multiple header checksums
        for (int i = 0; i < 100; i++)
        {
            var slice = _smallData.AsSpan(0, Math.Min(i + 10, _smallData.Length));
            _ = Crc32Calculator.Calculate(slice);
        }
    }
}
