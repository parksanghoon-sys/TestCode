using BenchmarkDotNet.Attributes;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using TcpSimulator.Application.Handlers;
using TcpSimulator.Application.Services;
using TcpSimulator.Domain.Entities;
using TcpSimulator.Domain.Interfaces;
using TcpSimulator.Domain.ValueObjects;
using TcpSimulator.Infrastructure.Buffers;
using TcpSimulator.Infrastructure.Network;
using TcpSimulator.Infrastructure.Protocol;

namespace TcpSimulator.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 5)]
public class EndToEndBenchmarks
{
    private Socket? _serverSocket;
    private TcpConnection? _serverConnection;
    private TcpConnection? _clientConnection;
    private readonly int _testPort = 19998;
    private readonly ConcurrentBag<IMessage> _receivedMessages = new();
    private readonly TextMessage _testMessage = new("Benchmark");

    [GlobalSetup]
    public async Task Setup()
    {
        // Setup server
        _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _serverSocket.Bind(new IPEndPoint(IPAddress.Loopback, _testPort));
        _serverSocket.Listen(1);

        // Setup client
        var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var connectTask = clientSocket.ConnectAsync(IPAddress.Loopback, _testPort);
        var acceptTask = _serverSocket.AcceptAsync();

        await Task.WhenAll(connectTask, acceptTask);

        // Create connections
        var serializer = new BibeProtocolSerializer();
        var bufferManager = new PooledBufferManager();

        // Server dispatcher
        var serverDispatcher = new MessageDispatcher();
        serverDispatcher.RegisterHandler(new BenchmarkMessageCollector<TextMessage>(_receivedMessages));
        serverDispatcher.RegisterHandler(new BenchmarkMessageCollector<EchoMessage>(_receivedMessages));

        // Client dispatcher
        var clientDispatcher = new MessageDispatcher();
        clientDispatcher.RegisterHandler(new BenchmarkMessageCollector<TextMessage>(_receivedMessages));
        clientDispatcher.RegisterHandler(new BenchmarkMessageCollector<EchoMessage>(_receivedMessages));

        _serverConnection = new TcpConnection(acceptTask.Result, serializer, bufferManager, serverDispatcher);
        _clientConnection = new TcpConnection(clientSocket, serializer, bufferManager, clientDispatcher);

        _serverConnection.Start();
        _clientConnection.Start();

        await Task.Delay(100); // Connection stabilization
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _clientConnection?.Dispose();
        _serverConnection?.Dispose();
        _serverSocket?.Dispose();
    }

    [Benchmark]
    public async Task SendReceive_SingleMessage()
    {
        _receivedMessages.Clear();
        await _clientConnection!.SendAsync(_testMessage);

        // Wait for message to be received
        var timeout = DateTime.UtcNow.AddSeconds(1);
        while (_receivedMessages.IsEmpty && DateTime.UtcNow < timeout)
        {
            await Task.Delay(1);
        }
    }

    [Benchmark]
    public async Task SendReceive_10Messages()
    {
        _receivedMessages.Clear();
        var tasks = new Task[10];
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = _clientConnection!.SendAsync(_testMessage).AsTask();
        }
        await Task.WhenAll(tasks);

        // Wait for all messages
        var timeout = DateTime.UtcNow.AddSeconds(2);
        while (_receivedMessages.Count < 10 && DateTime.UtcNow < timeout)
        {
            await Task.Delay(1);
        }
    }

    [Benchmark]
    public async Task SendReceive_BidirectionalMessages()
    {
        _receivedMessages.Clear();
        var tasks = new[]
        {
            _clientConnection!.SendAsync(_testMessage).AsTask(),
            _serverConnection!.SendAsync(_testMessage).AsTask()
        };
        await Task.WhenAll(tasks);

        var timeout = DateTime.UtcNow.AddSeconds(1);
        while (_receivedMessages.Count < 2 && DateTime.UtcNow < timeout)
        {
            await Task.Delay(1);
        }
    }

    [Benchmark]
    public async Task Latency_RoundTrip()
    {
        _receivedMessages.Clear();
        var start = DateTime.UtcNow;

        await _clientConnection!.SendAsync(_testMessage);

        var timeout = start.AddSeconds(1);
        while (_receivedMessages.IsEmpty && DateTime.UtcNow < timeout)
        {
            await Task.Delay(1);
        }

        var latency = DateTime.UtcNow - start;
        // Latency recorded in benchmark results
    }

    // Helper class
    private class BenchmarkMessageCollector<TMessage> : IMessageHandler<TMessage>
        where TMessage : IMessage
    {
        private readonly ConcurrentBag<IMessage> _messages;

        public BenchmarkMessageCollector(ConcurrentBag<IMessage> messages)
        {
            _messages = messages;
        }

        public Task HandleAsync(TMessage message, CancellationToken cancellationToken)
        {
            _messages.Add(message);
            return Task.CompletedTask;
        }

        public bool CanHandle(byte messageType)
        {
            if (typeof(TMessage) == typeof(TextMessage))
                return messageType == MessageType.Text;
            if (typeof(TMessage) == typeof(EchoMessage))
                return messageType == MessageType.Echo;
            return false;
        }
    }
}
