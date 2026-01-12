using System.Net.Sockets;
using System.Threading.Channels;
using TcpSimulator.Domain.Interfaces;

namespace TcpSimulator.Infrastructure.Network;

public sealed class TcpSender : IDisposable
{
    private readonly Socket _socket;
    private readonly IProtocolSerializer _serializer;
    private readonly IBufferManager _bufferManager;
    private readonly Channel<IMessage> _sendQueue;
    private readonly CancellationTokenSource _cts;
    private readonly Thread _sendThread;

    public TcpSender(
        Socket socket,
        IProtocolSerializer serializer,
        IBufferManager bufferManager)
    {
        _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _bufferManager = bufferManager ?? throw new ArgumentNullException(nameof(bufferManager));

        _sendQueue = Channel.CreateUnbounded<IMessage>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        _cts = new CancellationTokenSource();
        _sendThread = new Thread(SendLoop) { IsBackground = true, Name = "TcpSender" };
    }

    public void Start() => _sendThread.Start();

    public async ValueTask EnqueueAsync(IMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        await _sendQueue.Writer.WriteAsync(message, cancellationToken);
    }

    private void SendLoop()
    {
        using var buffer = _bufferManager.Rent(8192);
        var token = _cts.Token;

        try
        {
            while (!token.IsCancellationRequested)
            {
                // Channel에서 메시지 읽기 (blocking)
                if (!_sendQueue.Reader.TryRead(out var message))
                {
                    // 메시지 없으면 대기
                    if (!_sendQueue.Reader.WaitToReadAsync(token).AsTask().Result)
                        break;
                    continue;
                }

                // 메시지 직렬화
                int written = _serializer.Serialize(message, buffer.Span);
                if (written <= 0)
                    continue; // 직렬화 실패

                // 전송 (동기 방식, 전용 스레드이므로 blocking 허용)
                int sent = 0;
                while (sent < written)
                {
                    int bytesTransferred = _socket.Send(
                        buffer.Span.Slice(sent, written - sent),
                        SocketFlags.None);

                    if (bytesTransferred <= 0)
                        throw new SocketException();

                    sent += bytesTransferred;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 정상 종료
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TcpSender] Error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _sendQueue.Writer.Complete();

        if (_sendThread.IsAlive)
        {
            _sendThread.Join(TimeSpan.FromSeconds(5));
        }

        _cts.Dispose();
    }
}
