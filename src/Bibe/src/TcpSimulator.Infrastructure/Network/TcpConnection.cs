using System.Net.Sockets;
using TcpSimulator.Domain.Interfaces;

namespace TcpSimulator.Infrastructure.Network;

public sealed class TcpConnection : IDisposable
{
    private readonly Socket _socket;
    private readonly TcpSender _sender;
    private readonly TcpReceiver _receiver;

    public string RemoteEndPoint => _socket.RemoteEndPoint?.ToString() ?? "Unknown";
    public bool IsConnected => _socket.Connected;

    public TcpConnection(
        Socket socket,
        IProtocolSerializer serializer,
        IBufferManager bufferManager,
        IMessageDispatcher dispatcher)
    {
        _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        _sender = new TcpSender(socket, serializer, bufferManager);
        _receiver = new TcpReceiver(socket, serializer, bufferManager, dispatcher);
    }

    public void Start()
    {
        _sender.Start();
        _receiver.Start();
    }

    public ValueTask SendAsync(IMessage message, CancellationToken cancellationToken = default)
    {
        return _sender.EnqueueAsync(message, cancellationToken);
    }

    public void Dispose()
    {
        _receiver.Dispose();
        _sender.Dispose();

        try
        {
            _socket.Shutdown(SocketShutdown.Both);
        }
        catch
        {
            // Ignore shutdown errors
        }

        _socket.Dispose();
    }
}
