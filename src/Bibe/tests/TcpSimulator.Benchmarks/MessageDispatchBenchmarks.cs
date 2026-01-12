using BenchmarkDotNet.Attributes;
using TcpSimulator.Application.Handlers;
using TcpSimulator.Application.Services;
using TcpSimulator.Domain.Entities;
using TcpSimulator.Domain.Interfaces;

namespace TcpSimulator.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class MessageDispatchBenchmarks
{
    private readonly IMessageDispatcher _dispatcher = new MessageDispatcher();
    private readonly TextMessage _textMessage = new("Benchmark message");
    private readonly EchoMessage _echoMessage = new("Echo data"u8.ToArray());
    private readonly HeartbeatMessage _heartbeatMessage = new(12345L);
    private readonly CancellationToken _cancellationToken = CancellationToken.None;
    private int _handlerCallCount = 0;

    [GlobalSetup]
    public void Setup()
    {
        // Register handlers
        _dispatcher.RegisterHandler(new TextMessageHandler());
        _dispatcher.RegisterHandler(new EchoMessageHandler(async () =>
        {
            Interlocked.Increment(ref _handlerCallCount);
            await Task.CompletedTask;
        }));
        _dispatcher.RegisterHandler(new HeartbeatMessageHandler());
    }

    [Benchmark]
    public async Task Dispatch_TextMessage()
    {
        await _dispatcher.DispatchAsync(_textMessage, _cancellationToken);
    }

    [Benchmark]
    public async Task Dispatch_EchoMessage()
    {
        await _dispatcher.DispatchAsync(_echoMessage, _cancellationToken);
    }

    [Benchmark]
    public async Task Dispatch_HeartbeatMessage()
    {
        await _dispatcher.DispatchAsync(_heartbeatMessage, _cancellationToken);
    }

    [Benchmark]
    public async Task Dispatch_MultipleMessages_Sequential()
    {
        for (int i = 0; i < 10; i++)
        {
            await _dispatcher.DispatchAsync(_textMessage, _cancellationToken);
            await _dispatcher.DispatchAsync(_echoMessage, _cancellationToken);
            await _dispatcher.DispatchAsync(_heartbeatMessage, _cancellationToken);
        }
    }

    [Benchmark]
    public async Task Dispatch_MultipleMessages_Parallel()
    {
        var tasks = new Task[30];
        for (int i = 0; i < 10; i++)
        {
            tasks[i * 3] = _dispatcher.DispatchAsync(_textMessage, _cancellationToken);
            tasks[i * 3 + 1] = _dispatcher.DispatchAsync(_echoMessage, _cancellationToken);
            tasks[i * 3 + 2] = _dispatcher.DispatchAsync(_heartbeatMessage, _cancellationToken);
        }
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task Dispatch_UnknownMessageType()
    {
        var unknownMessage = new CustomMessage(0xFF, "Unknown"u8.ToArray());
        await _dispatcher.DispatchAsync(unknownMessage, _cancellationToken);
    }

    // Helper for unknown message type test
    private class CustomMessage : IMessage
    {
        public byte Type { get; }
        public ReadOnlyMemory<byte> Payload { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;

        public CustomMessage(byte type, byte[] payload)
        {
            Type = type;
            Payload = payload;
        }
    }
}
