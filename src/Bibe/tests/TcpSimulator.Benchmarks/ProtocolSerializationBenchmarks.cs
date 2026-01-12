using BenchmarkDotNet.Attributes;
using TcpSimulator.Domain.Entities;
using TcpSimulator.Domain.Interfaces;
using TcpSimulator.Infrastructure.Protocol;

namespace TcpSimulator.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class ProtocolSerializationBenchmarks
{
    private readonly IProtocolSerializer _serializer = new BibeProtocolSerializer();
    private readonly TextMessage _smallMessage = new("Hello");
    private readonly TextMessage _mediumMessage = new(new string('A', 1024));
    private readonly TextMessage _largeMessage = new(new string('B', 10240));
    private readonly byte[] _buffer = new byte[16384];
    private byte[] _serializedSmall = Array.Empty<byte>();
    private byte[] _serializedMedium = Array.Empty<byte>();
    private byte[] _serializedLarge = Array.Empty<byte>();

    [GlobalSetup]
    public void Setup()
    {
        // Pre-serialize messages for deserialization benchmarks
        _serializedSmall = new byte[_serializer.Serialize(_smallMessage, _buffer)];
        Array.Copy(_buffer, _serializedSmall, _serializedSmall.Length);

        _serializedMedium = new byte[_serializer.Serialize(_mediumMessage, _buffer)];
        Array.Copy(_buffer, _serializedMedium, _serializedMedium.Length);

        _serializedLarge = new byte[_serializer.Serialize(_largeMessage, _buffer)];
        Array.Copy(_buffer, _serializedLarge, _serializedLarge.Length);
    }

    [Benchmark]
    public int Serialize_SmallMessage()
    {
        return _serializer.Serialize(_smallMessage, _buffer);
    }

    [Benchmark]
    public int Serialize_MediumMessage()
    {
        return _serializer.Serialize(_mediumMessage, _buffer);
    }

    [Benchmark]
    public int Serialize_LargeMessage()
    {
        return _serializer.Serialize(_largeMessage, _buffer);
    }

    [Benchmark]
    public IMessage? Deserialize_SmallMessage()
    {
        return _serializer.Deserialize(_serializedSmall);
    }

    [Benchmark]
    public IMessage? Deserialize_MediumMessage()
    {
        return _serializer.Deserialize(_serializedMedium);
    }

    [Benchmark]
    public IMessage? Deserialize_LargeMessage()
    {
        return _serializer.Deserialize(_serializedLarge);
    }

    [Benchmark]
    public bool IsCompleteMessage_Valid()
    {
        return _serializer.IsCompleteMessage(_serializedSmall, out _);
    }

    [Benchmark]
    public bool IsCompleteMessage_Incomplete()
    {
        var incomplete = _serializedMedium.AsSpan(0, 10);
        return _serializer.IsCompleteMessage(incomplete, out _);
    }
}
