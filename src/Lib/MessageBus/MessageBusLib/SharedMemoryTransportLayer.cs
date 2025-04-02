using MessageBusLib.Exceptions;
using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;

namespace MessageBusLib;

/// <summary>
/// 공유 메모리 기반 전송 계층 구현 (기존 방식)
/// </summary>
/// <summary>
/// 공유 메모리 기반 전송 계층 구현
/// </summary>
public class SharedMemoryTransportLayer : ITransportLayer
{
    private readonly SharedMemoryTransportOptions _options;
    private readonly Mutex _mutex;
    private readonly EventWaitHandle _messageEvent;
    private readonly MemoryMappedFile _mmf;
    private readonly MemoryMappedViewAccessor _accessor;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _receiveTask;
    private readonly ConcurrentQueue<byte[]> _receiveQueue;
    private bool _isRunning = false;
    private bool _disposed = false;

    /// <summary>
    /// 메시지 수신 이벤트
    /// </summary>
    public event EventHandler<TransportMessageReceivedEventArgs> MessageReceived;

    /// <summary>
    /// 공유 메모리 전송 계층 초기화
    /// </summary>
    /// <param name="options">구성 옵션</param>
    public SharedMemoryTransportLayer(SharedMemoryTransportOptions options = null)
    {
        _options = options ?? new SharedMemoryTransportOptions();
        _cancellationTokenSource = new CancellationTokenSource();
        _receiveQueue = new ConcurrentQueue<byte[]>();

        bool createdNew;

        try
        {
            // 뮤텍스 생성/열기
            _mutex = new Mutex(false, _options.MutexName, out createdNew);

            // 이벤트 생성/열기
            _messageEvent = new EventWaitHandle(false, EventResetMode.AutoReset, _options.EventName, out createdNew);

            // 메모리 매핑 파일 생성/열기
            _mmf = MemoryMappedFile.CreateOrOpen(_options.MemoryMappedFileName, _options.BufferSize);
            _accessor = _mmf.CreateViewAccessor();

            // 수신 태스크 초기화 (아직 시작하지 않음)
            _receiveTask = new Task(ReceiveLoop, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning);
        }
        catch (Exception ex)
        {
            // 초기화 중에 생성된 자원 정리
            _mutex?.Dispose();
            _messageEvent?.Dispose();
            _mmf?.Dispose();
            _accessor?.Dispose();
            _cancellationTokenSource.Dispose();

            throw new MessageBusException("공유 메모리 전송 계층 초기화 중 오류 발생", ex);
        }
    }

    /// <summary>
    /// 메시지 전송
    /// </summary>
    public void SendMessage(byte[] data)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SharedMemoryTransportLayer));

        if (data == null || data.Length == 0)
            return;

        if (data.Length > _options.MaxMessageSize)
            throw new ArgumentException($"메시지 크기가 최대 허용 크기({_options.MaxMessageSize} 바이트)를 초과합니다.");

        try
        {
            _mutex.WaitOne();

            // 버퍼에 쓸 위치 결정 (원형 버퍼)
            long position = 0;
            _accessor.Read(0, out position);

            // 메시지 길이 쓰기
            _accessor.Write(8 + position, data.Length);

            // 메시지 내용 쓰기
            _accessor.WriteArray(12 + position, data, 0, data.Length);

            // 다음 쓰기 위치 업데이트 (원형 버퍼)
            position = (position + data.Length + 12) % (_options.BufferSize - 12);
            _accessor.Write(0, position);

            // 대기 중인 프로세스에 알림
            _messageEvent.Set();
        }
        catch (Exception ex)
        {
            throw new MessageBusException("메시지 전송 중 오류 발생", ex);
        }
        finally
        {
            try { _mutex.ReleaseMutex(); } catch { }
        }
    }

    /// <summary>
    /// 전송 계층 시작
    /// </summary>
    public void Start()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SharedMemoryTransportLayer));

        if (_isRunning)
            return;

        _isRunning = true;
        _receiveTask.Start();

        // 메시지 처리 태스크 시작
        Task.Run(ProcessQueueLoop, _cancellationTokenSource.Token);
    }

    /// <summary>
    /// 전송 계층 중지
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
        _cancellationTokenSource.Cancel();
        _messageEvent.Set(); // 대기 중인 수신 루프 깨우기
    }

    /// <summary>
    /// 메시지 수신 루프
    /// </summary>
    private void ReceiveLoop()
    {
        // 마지막으로 읽은 위치
        long lastReadPosition = 0;

        while (_isRunning && !_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                // 새 메시지가 있을 때까지 대기
                _messageEvent.WaitOne(_options.MessageReadInterval);

                if (!_isRunning || _cancellationTokenSource.Token.IsCancellationRequested)
                    break;

                _mutex.WaitOne();

                try
                {
                    // 현재 쓰기 위치 읽기
                    long writePosition = 0;
                    _accessor.Read(0, out writePosition);

                    // 읽을 메시지가 있는지 확인
                    if (writePosition != lastReadPosition)
                    {
                        // 모든 메시지 읽기
                        while (lastReadPosition != writePosition)
                        {
                            // 메시지 길이 읽기
                            int messageLength = 0;
                            _accessor.Read(8 + lastReadPosition, out messageLength);

                            if (messageLength <= 0 || messageLength > _options.MaxMessageSize)
                            {
                                // 잘못된 메시지 길이, 버퍼 손상 가능성
                                lastReadPosition = writePosition;
                                break;
                            }

                            // 메시지 내용 읽기
                            byte[] messageBytes = new byte[messageLength];
                            _accessor.ReadArray(12 + lastReadPosition, messageBytes, 0, messageLength);

                            // 다음 읽기 위치 계산 (원형 버퍼)
                            lastReadPosition = (lastReadPosition + messageLength + 12) % (_options.BufferSize - 12);

                            // 수신 큐에 추가
                            _receiveQueue.Enqueue(messageBytes);
                        }
                    }
                }
                finally
                {
                    try { _mutex.ReleaseMutex(); } catch { }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"메시지 수신 중 오류: {ex.Message}");
                Thread.Sleep(100); // 연속적인 오류 방지
            }
        }
    }

    /// <summary>
    /// 수신 큐 처리 루프
    /// </summary>
    private void ProcessQueueLoop()
    {
        while (_isRunning && !_cancellationTokenSource.Token.IsCancellationRequested)
        {
            while (_receiveQueue.TryDequeue(out byte[] data))
            {
                try
                {
                    // 메시지 수신 이벤트 발생
                    OnMessageReceived(new TransportMessageReceivedEventArgs(data));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"메시지 이벤트 처리 중 오류: {ex.Message}");
                }
            }

            // 큐가 비어있을 때 CPU 사용량 절약
            if (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                Thread.Sleep(10);
            }
        }
    }

    /// <summary>
    /// 메시지 수신 이벤트 발생
    /// </summary>
    protected virtual void OnMessageReceived(TransportMessageReceivedEventArgs args)
    {
        MessageReceived?.Invoke(this, args);
    }

    /// <summary>
    /// 자원 해제
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // 정리 중
                Stop();

                try
                {
                    if (_receiveTask != null && !_receiveTask.IsCompleted)
                    {
                        Task.WaitAny(new[] { _receiveTask }, 1000);
                    }
                }
                catch { }

                _accessor?.Dispose();
                _mmf?.Dispose();
                _messageEvent?.Dispose();
                _mutex?.Dispose();
                _cancellationTokenSource.Dispose();
            }

            _disposed = true;
        }
    }

    ~SharedMemoryTransportLayer()
    {
        Dispose(false);
    }
}
