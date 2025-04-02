using MessageBusLib.Exceptions;
using MessageBusLib.Messages;
using MessageBusLib.Serialization;
using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;

namespace MessageBusLib;

public class MessageBus : IMessageBus
{
    private readonly MessageBusOptions _options;
    private readonly ConcurrentDictionary<string, List<MessageHandler>> _subscriptions;
    private readonly ConcurrentQueue<IMessage> _messageQueue;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _processingTask;
    private readonly Mutex _mutex;
    private readonly EventWaitHandle _messageEvent;
    private readonly MemoryMappedFile _mmf;
    private readonly MemoryMappedViewAccessor _accessor;
    private readonly ISerializer _serializer;
    private bool _disposed = false;

    /// <summary>
    /// 메시지 수신 이벤트
    /// </summary>
    public event EventHandler<IMessageReceivedEventArgs> MessageReceived;

    /// <summary>
    /// 메시지 버스 초기화
    /// </summary>
    /// <param name="options">메시지 버스 구성 옵션</param>
    /// <param name="serializer">사용할 직렬화 도구</param>
    public MessageBus(MessageBusOptions options = null, ISerializer serializer = null)
    {
        _options = options ?? new MessageBusOptions();
        _serializer = serializer ?? new JsonSerializer();
        _subscriptions = new ConcurrentDictionary<string, List<MessageHandler>>();
        _messageQueue = new ConcurrentQueue<IMessage>();
        _cancellationTokenSource = new CancellationTokenSource();

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

            // 메시지 처리 태스크 시작
            _processingTask = Task.Factory.StartNew(ProcessMessages,
                _cancellationTokenSource.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            // 프로세스 종료 시 자원 해제
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Dispose();
        }
        catch (Exception ex)
        {
            // 초기화 중에 생성된 자원 정리
            _mutex?.Dispose();
            _messageEvent?.Dispose();
            _mmf?.Dispose();
            _accessor?.Dispose();
            _cancellationTokenSource?.Dispose();

            throw new MessageBusException("메시지 버스 초기화 중 오류 발생", ex);
        }
    }
    /// <summary>
    /// 메시지 버스 초기화
    /// </summary>
    /// <param name="busName">버스 이름 (동일 이름의 버스는 서로 통신 가능)</param>
    public MessageBus(string busName)
        : this(new MessageBusOptions { BusName = busName })
    {
    }
    /// <summary>
    /// 지정된 토픽에 메시지 발행
    /// </summary>
    /// <param name="topic">메시지 토픽</param>
    /// <param name="data">메시지 데이터</param>
    /// <returns>발행된 메시지 ID</returns>
    public string Publish(string topic, object data)
    {
        if (string.IsNullOrEmpty(topic))
            throw new ArgumentNullException(nameof(topic));

        var message = new Message(topic, data);
        PublishMessage(message);
        return message.MessageId;
    }
    /// <summary>
    /// 지정된 ID로 메시지 발행
    /// </summary>
    /// <param name="messageId">메시지 ID</param>
    /// <param name="topic">메시지 토픽</param>
    /// <param name="data">메시지 데이터</param>
    public void PublishWithId(string messageId, string topic, object data)
    {
        if (string.IsNullOrEmpty(topic))
            throw new ArgumentNullException(nameof(topic));
        if (string.IsNullOrEmpty(messageId))
            throw new ArgumentNullException(nameof(messageId));

        var message = new Message(topic, data, messageId);
        PublishMessage(message);
    }
    /// <summary>
    /// 지정된 토픽 구독
    /// </summary>
    /// <param name="topic">구독할 토픽</param>
    /// <param name="handler">메시지 처리 핸들러</param>
    public void Subscribe(string topic, MessageHandler handler)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MessageBus));

        if (string.IsNullOrEmpty(topic))
            throw new ArgumentNullException(nameof(topic));
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        _subscriptions.AddOrUpdate(topic,
            new List<MessageHandler> { handler },
            (_, handlers) =>
            {
                handlers.Add(handler);
                return handlers;
            });
    }
    /// <summary>
    /// 지정된 토픽 구독 해제
    /// </summary>
    /// <param name="topic">구독 해제할 토픽</param>
    /// <param name="handler">제거할 핸들러 (null이면 모든 핸들러 제거)</param>
    public void Unsubscribe(string topic, MessageHandler handler = null)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MessageBus));

        if (string.IsNullOrEmpty(topic))
            throw new ArgumentNullException(nameof(topic));

        if (handler == null)
        {
            _subscriptions.TryRemove(topic, out _);
        }
        else if (_subscriptions.TryGetValue(topic, out var handlers))
        {
            handlers.Remove(handler);
            if (handlers.Count == 0)
            {
                _subscriptions.TryRemove(topic, out _);
            }
        }
    }
    /// <summary>
    /// 메시지 객체 발행
    /// </summary>
    private void PublishMessage(Message message)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MessageBus));

        try
        {
            _mutex.WaitOne();

            // 메시지 직렬화
            byte[] messageBytes = _serializer.Serialize(message);

            // 메시지 길이 확인
            int messageLength = messageBytes.Length;

            if (messageLength > _options.MaxMessageSize)
            {
                throw new MessageBusException($"메시지 크기가 최대 허용 크기({_options.MaxMessageSize} 바이트)를 초과합니다.");
            }

            // 버퍼에 쓸 위치 결정 (원형 버퍼)
            long position = 0;
            _accessor.Read(0, out position);

            // 메시지 길이 쓰기
            _accessor.Write(8 + position, messageLength);

            // 메시지 내용 쓰기
            _accessor.WriteArray(12 + position, messageBytes, 0, messageLength);

            // 다음 쓰기 위치 업데이트
            position = (position + messageLength + 12) % (_options.BufferSize - 12);
            _accessor.Write(0, position);

            // 대기 중인 프로세스에 알림
            _messageEvent.Set();
        }
        catch (Exception ex)
        {
            throw new MessageBusException("메시지 발행 중 오류 발생", ex);
        }
        finally
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch
            {
                // 이미 해제된 뮤텍스일 수 있으므로 무시
            }
        }
    }
    /// <summary>
    /// 메시지 처리 루프
    /// </summary>
    private void ProcessMessages()
    {
        // 마지막으로 읽은 위치
        long lastReadPosition = 0;

        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                // 새 메시지가 있을 때까지 대기
                _messageEvent.WaitOne(_options.MessageReadInterval);

                if (_cancellationTokenSource.Token.IsCancellationRequested)
                    break;

                _mutex.WaitOne();

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

                        // 다음 읽기 위치 계산
                        lastReadPosition = (lastReadPosition + messageLength + 12) % (_options.BufferSize - 12);

                        try
                        {
                            // 메시지 역직렬화 및 처리
                            Message message = _serializer.Deserialize<Message>(messageBytes);

                            // 자신이 보낸 메시지는 무시 (선택적)
                            // if (message.SenderId == Process.GetCurrentProcess().Id)
                            //    continue;

                            _messageQueue.Enqueue(message);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"메시지 역직렬화 오류: {ex.Message}");
                            // 손상된 메시지는 건너뜀
                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"메시지 처리 오류: {ex.Message}");
            }
            finally
            {
                try
                {
                    _mutex.ReleaseMutex();
                }
                catch
                {
                    // 이미 해제된 뮤텍스일 수 있으므로 무시
                }
            }

            if (_cancellationTokenSource.Token.IsCancellationRequested)
                break;

            // 큐에 있는 메시지 처리
            ProcessQueuedMessages();
        }
    }

    /// <summary>
    /// 큐에 있는 메시지를 처리
    /// </summary>
    private void ProcessQueuedMessages()
    {
        while (_messageQueue.TryDequeue(out var message))
        {
            try
            {
                // 전체 메시지 이벤트 발생
                OnMessageReceived(message);

                // 구독자에게 메시지 전달
                if (_subscriptions.TryGetValue(message.Topic, out var handlers))
                {
                    var eventArgs = new MessageReceivedEventArgs(message);
                    foreach (var handler in handlers.ToList()) // ToList로 복사본 사용
                    {
                        try
                        {
                            handler(this, eventArgs);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"핸들러 실행 오류: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"메시지 이벤트 처리 오류: {ex.Message}");
            }
        }
    }
    /// <summary>
    /// 메시지 수신 이벤트 발생
    /// </summary>
    protected virtual void OnMessageReceived(IMessage message)
    {
        MessageReceived?.Invoke(this, new MessageReceivedEventArgs(message));
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
                // 관리 자원 해제
                _cancellationTokenSource.Cancel();

                try
                {
                    if (_processingTask != null && !_processingTask.IsCompleted)
                    {
                        _processingTask.Wait(1000);
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

}