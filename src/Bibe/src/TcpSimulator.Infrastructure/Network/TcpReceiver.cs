using System.Net.Sockets;
using TcpSimulator.Domain.Interfaces;
using TcpSimulator.Infrastructure.Buffers;

namespace TcpSimulator.Infrastructure.Network;

public sealed class TcpReceiver : IDisposable
{
    private readonly Socket _socket;
    private readonly IProtocolSerializer _serializer;
    private readonly IBufferManager _bufferManager;
    private readonly IMessageDispatcher _dispatcher;
    private readonly CancellationTokenSource _cts;
    private readonly Thread _receiveThread;

    public TcpReceiver(
        Socket socket,
        IProtocolSerializer serializer,
        IBufferManager bufferManager,
        IMessageDispatcher dispatcher)
    {
        _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _bufferManager = bufferManager ?? throw new ArgumentNullException(nameof(bufferManager));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

        _cts = new CancellationTokenSource();
        _receiveThread = new Thread(ReceiveLoop)
        {
            IsBackground = true,
            Name = "TcpReceiver"
        };
    }

    public void Start() => _receiveThread.Start();

    private void ReceiveLoop()
    {
        using var buffer = _bufferManager.Rent(8192);
        using var assembler = new StreamBufferAssembler(_serializer, _bufferManager);
        var token = _cts.Token;

        try
        {
            while (!token.IsCancellationRequested)
            {
                // 데이터 수신 (blocking)
                int received = _socket.Receive(buffer.Span, SocketFlags.None);

                if (received <= 0)
                    break; // 연결 종료

                // 수신 데이터에서 메시지 조립
                var receivedSpan = buffer.Span.Slice(0, received);

                // 여러 메시지가 한 번에 올 수 있으므로 루프
                while (true)
                {
                    var message = assembler.TryAssembleMessage(receivedSpan);
                    if (message == null)
                        break; // 완전한 메시지 없음

                    // 메시지 처리 (비동기 디스패치)
                    _ = _dispatcher.DispatchAsync(message, token);

                    // 다음 메시지를 위해 span 비우기
                    receivedSpan = Span<byte>.Empty;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 정상 종료
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TcpReceiver] Error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _cts.Cancel();

        if (_receiveThread.IsAlive)
        {
            _receiveThread.Join(TimeSpan.FromSeconds(5));
        }

        _cts.Dispose();
    }
}
