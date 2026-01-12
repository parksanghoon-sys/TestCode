using System.Net;
using System.Net.Sockets;
using TcpSimulator.Application.Handlers;
using TcpSimulator.Application.Services;
using TcpSimulator.Domain.Entities;
using TcpSimulator.Domain.Interfaces;
using TcpSimulator.Infrastructure.Buffers;
using TcpSimulator.Infrastructure.Network;
using TcpSimulator.Infrastructure.Protocol;

Console.WriteLine("=== TCP Simulator Server ===");
Console.WriteLine();

// Configuration
var host = "127.0.0.1";
var port = 9000;

// Services
IProtocolSerializer serializer = new BibeProtocolSerializer();
IBufferManager bufferManager = new PooledBufferManager();
IMessageDispatcher dispatcher = new MessageDispatcher();

// Register Handlers
dispatcher.RegisterHandler(new TextMessageHandler());
dispatcher.RegisterHandler(new HeartbeatMessageHandler());

// Start Server
var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
listener.Bind(new IPEndPoint(IPAddress.Parse(host), port));
listener.Listen(100);

Console.WriteLine($"[Server] Listening on {host}:{port}");
Console.WriteLine("Press Ctrl+C to stop server...");
Console.WriteLine();

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// Accept connections
var connections = new List<TcpConnection>();

_ = Task.Run(async () =>
{
    while (!cts.Token.IsCancellationRequested)
    {
        try
        {
            var clientSocket = await listener.AcceptAsync(cts.Token);
            Console.WriteLine($"[Server] Client connected: {clientSocket.RemoteEndPoint}");

            // Create connection with Echo handler
            var connection = new TcpConnection(
                clientSocket,
                serializer,
                bufferManager,
                dispatcher);

            // Register echo handler for this connection
            dispatcher.RegisterHandler(new EchoMessageHandler(async () =>
            {
                // Echo back the message
                var echoMessage = new EchoMessage("Echo response"u8.ToArray());
                await connection.SendAsync(echoMessage);
            }));

            connection.Start();
            connections.Add(connection);
        }
        catch (OperationCanceledException)
        {
            break;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Server] Error accepting connection: {ex.Message}");
        }
    }
}, cts.Token);

// Wait for cancellation
cts.Token.WaitHandle.WaitOne();

Console.WriteLine();
Console.WriteLine("[Server] Shutting down...");

// Cleanup
foreach (var connection in connections)
{
    connection.Dispose();
}

listener.Dispose();

Console.WriteLine("[Server] Stopped.");
