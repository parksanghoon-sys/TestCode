using FluentAssertions;
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

namespace TcpSimulator.Integration.Tests;

public class TcpCommunicationIntegrationTests : IAsyncLifetime
{
    private Socket? _serverSocket;
    private TcpConnection? _serverConnection;
    private TcpConnection? _clientConnection;
    private readonly int _testPort = 19999; // Use different port to avoid conflicts
    private readonly ConcurrentBag<IMessage> _serverReceivedMessages = new();
    private readonly ConcurrentBag<IMessage> _clientReceivedMessages = new();

    public async Task InitializeAsync()
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
        serverDispatcher.RegisterHandler(new TestMessageCollector<TextMessage>(_serverReceivedMessages));
        serverDispatcher.RegisterHandler(new TestMessageCollector<EchoMessage>(_serverReceivedMessages));
        serverDispatcher.RegisterHandler(new TestMessageCollector<HeartbeatMessage>(_serverReceivedMessages));

        // Client dispatcher
        var clientDispatcher = new MessageDispatcher();
        clientDispatcher.RegisterHandler(new TestMessageCollector<TextMessage>(_clientReceivedMessages));
        clientDispatcher.RegisterHandler(new TestMessageCollector<EchoMessage>(_clientReceivedMessages));
        clientDispatcher.RegisterHandler(new TestMessageCollector<HeartbeatMessage>(_clientReceivedMessages));

        _serverConnection = new TcpConnection(acceptTask.Result, serializer, bufferManager, serverDispatcher);
        _clientConnection = new TcpConnection(clientSocket, serializer, bufferManager, clientDispatcher);

        _serverConnection.Start();
        _clientConnection.Start();

        // Give connections time to start
        await Task.Delay(100);
    }

    [Fact]
    public async Task ClientSendsTextMessage_ServerReceives()
    {
        // Arrange
        var message = new TextMessage("Hello Server");

        // Act
        await _clientConnection!.SendAsync(message);
        await Task.Delay(200); // Wait for transmission

        // Assert
        _serverReceivedMessages.Should().HaveCount(1);
        var received = _serverReceivedMessages.First();
        received.Type.Should().Be(MessageType.Text);
        (received as TextMessage)!.Text.Should().Be("Hello Server");
    }

    [Fact]
    public async Task ServerSendsTextMessage_ClientReceives()
    {
        // Arrange
        var message = new TextMessage("Hello Client");

        // Act
        await _serverConnection!.SendAsync(message);
        await Task.Delay(200);

        // Assert
        _clientReceivedMessages.Should().HaveCount(1);
        var received = _clientReceivedMessages.First();
        received.Type.Should().Be(MessageType.Text);
        (received as TextMessage)!.Text.Should().Be("Hello Client");
    }

    [Fact]
    public async Task MultipleMessages_AllReceived()
    {
        // Arrange
        var messages = new[]
        {
            new TextMessage("Message 1"),
            new TextMessage("Message 2"),
            new TextMessage("Message 3")
        };

        // Act
        foreach (var msg in messages)
        {
            await _clientConnection!.SendAsync(msg);
        }
        await Task.Delay(300);

        // Assert
        _serverReceivedMessages.Should().HaveCount(3);
        var receivedTexts = _serverReceivedMessages
            .OfType<TextMessage>()
            .Select(m => m.Text)
            .ToList();
        receivedTexts.Should().Contain("Message 1");
        receivedTexts.Should().Contain("Message 2");
        receivedTexts.Should().Contain("Message 3");
    }

    [Fact]
    public async Task EchoMessage_TransmittedCorrectly()
    {
        // Arrange
        var message = new EchoMessage("Echo test data"u8.ToArray());

        // Act
        await _clientConnection!.SendAsync(message);
        await Task.Delay(200);

        // Assert
        _serverReceivedMessages.Should().HaveCount(1);
        var received = _serverReceivedMessages.First();
        received.Type.Should().Be(MessageType.Echo);
    }

    [Fact]
    public async Task HeartbeatMessage_TransmittedCorrectly()
    {
        // Arrange
        var message = new HeartbeatMessage(12345L);

        // Act
        await _clientConnection!.SendAsync(message);
        await Task.Delay(200);

        // Assert
        _serverReceivedMessages.Should().HaveCount(1);
        var received = _serverReceivedMessages.First() as HeartbeatMessage;
        received.Should().NotBeNull();
        received!.SequenceNumber.Should().Be(12345L);
    }

    [Fact]
    public async Task BidirectionalCommunication_WorksCorrectly()
    {
        // Arrange
        var clientMessage = new TextMessage("From Client");
        var serverMessage = new TextMessage("From Server");

        // Act
        await _clientConnection!.SendAsync(clientMessage);
        await _serverConnection!.SendAsync(serverMessage);
        await Task.Delay(300);

        // Assert
        _serverReceivedMessages.Should().HaveCount(1);
        _clientReceivedMessages.Should().HaveCount(1);

        (_serverReceivedMessages.First() as TextMessage)!.Text.Should().Be("From Client");
        (_clientReceivedMessages.First() as TextMessage)!.Text.Should().Be("From Server");
    }

    [Fact]
    public async Task LargeMessage_TransmittedCorrectly()
    {
        // Arrange
        var largeText = new string('A', 5000); // 5KB message
        var message = new TextMessage(largeText);

        // Act
        await _clientConnection!.SendAsync(message);
        await Task.Delay(500);

        // Assert
        _serverReceivedMessages.Should().HaveCount(1);
        var received = _serverReceivedMessages.First() as TextMessage;
        received.Should().NotBeNull();
        received!.Text.Length.Should().Be(5000);
    }

    [Fact]
    public async Task RapidMessages_AllReceived()
    {
        // Arrange
        const int messageCount = 50;
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < messageCount; i++)
        {
            var msg = new TextMessage($"Rapid {i}");
            tasks.Add(_clientConnection!.SendAsync(msg).AsTask());
        }
        await Task.WhenAll(tasks);
        await Task.Delay(1000); // Wait for all to be received

        // Assert
        _serverReceivedMessages.Should().HaveCount(messageCount);
    }

    public async Task DisposeAsync()
    {
        _clientConnection?.Dispose();
        _serverConnection?.Dispose();
        _serverSocket?.Dispose();
        await Task.CompletedTask;
    }

    // Helper class to collect received messages
    private class TestMessageCollector<TMessage> : IMessageHandler<TMessage>
        where TMessage : IMessage
    {
        private readonly ConcurrentBag<IMessage> _messages;

        public TestMessageCollector(ConcurrentBag<IMessage> messages)
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
            if (typeof(TMessage) == typeof(HeartbeatMessage))
                return messageType == MessageType.Heartbeat;
            return false;
        }
    }
}
