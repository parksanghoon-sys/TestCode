using System.Net;
using System.Net.Sockets;
using TcpSimulator.Application.Handlers;
using TcpSimulator.Application.Services;
using TcpSimulator.Domain.Entities;
using TcpSimulator.Domain.Interfaces;
using TcpSimulator.Infrastructure.Buffers;
using TcpSimulator.Infrastructure.Network;
using TcpSimulator.Infrastructure.Protocol;

Console.WriteLine("=== TCP Simulator Client ===");
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
dispatcher.RegisterHandler(new EchoMessageHandler(async () => { /* Echo received */ }));
dispatcher.RegisterHandler(new HeartbeatMessageHandler());

// Connect to server
Console.WriteLine($"[Client] Connecting to {host}:{port}...");

var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

try
{
    await socket.ConnectAsync(new IPEndPoint(IPAddress.Parse(host), port));
    Console.WriteLine($"[Client] Connected to server!");
    Console.WriteLine();
}
catch (Exception ex)
{
    Console.WriteLine($"[Client] Connection failed: {ex.Message}");
    return;
}

// Create connection
var connection = new TcpConnection(socket, serializer, bufferManager, dispatcher);
connection.Start();

Console.WriteLine("Commands:");
Console.WriteLine("  text <message>  - Send text message");
Console.WriteLine("  echo <message>  - Send echo message");
Console.WriteLine("  heartbeat <seq> - Send heartbeat");
Console.WriteLine("  quit            - Exit");
Console.WriteLine();

// Interactive loop
while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
        continue;

    var parts = input.Split(' ', 2);
    var command = parts[0].ToLower();

    try
    {
        switch (command)
        {
            case "text":
                if (parts.Length > 1)
                {
                    var textMessage = new TextMessage(parts[1]);
                    await connection.SendAsync(textMessage);
                    Console.WriteLine("[Client] Text message sent");
                }
                else
                {
                    Console.WriteLine("[Client] Usage: text <message>");
                }
                break;

            case "echo":
                if (parts.Length > 1)
                {
                    var echoMessage = new EchoMessage(System.Text.Encoding.UTF8.GetBytes(parts[1]));
                    await connection.SendAsync(echoMessage);
                    Console.WriteLine("[Client] Echo message sent");
                }
                else
                {
                    Console.WriteLine("[Client] Usage: echo <message>");
                }
                break;

            case "heartbeat":
                if (parts.Length > 1 && long.TryParse(parts[1], out long seq))
                {
                    var heartbeatMessage = new HeartbeatMessage(seq);
                    await connection.SendAsync(heartbeatMessage);
                    Console.WriteLine("[Client] Heartbeat sent");
                }
                else
                {
                    Console.WriteLine("[Client] Usage: heartbeat <sequence_number>");
                }
                break;

            case "quit":
            case "exit":
                Console.WriteLine("[Client] Disconnecting...");
                connection.Dispose();
                return;

            default:
                Console.WriteLine("[Client] Unknown command");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Client] Error: {ex.Message}");
    }
}
