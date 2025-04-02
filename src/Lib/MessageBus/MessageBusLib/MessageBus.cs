using MessageBusLib.Exceptions;
using MessageBusLib.Messages;
using MessageBusLib.Serialization;
using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;

namespace MessageBusLib;

/// <summary>
/// 개선된 전송 계층 기반 메시지 버스
/// </summary>
public class MessageBus : IMessageBus
{
    private readonly ITransportOptions _options;
    private readonly ConcurrentDictionary<string, List<MessageHandler>> _subscriptions;
    private readonly ConcurrentQueue<IMessage> _messageQueue;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _processingTask;
    private readonly ISerializer _serializer;
    private readonly ITransportLayer _transportLayer;
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
    /// <param name="transportLayer">사용할 전송 계층</param>
    public MessageBus(
        ITransportOptions options,
        ISerializer serializer = null,
        ITransportLayer transportLayer = null)
    {
        _options = options;
        _serializer = serializer ?? new JsonSerializer();
        _transportLayer = transportLayer ?? CreateDefaultTransportLayer(_options);
        _subscriptions = new ConcurrentDictionary<string, List<MessageHandler>>();
        _messageQueue = new ConcurrentQueue<IMessage>();
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            // 전송 계층 메시지 수신 이벤트 구독
            _transportLayer.MessageReceived += OnTransportMessageReceived;

            // 전송 계층 시작
            _transportLayer.Start();

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
            _transportLayer?.Dispose();
            _cancellationTokenSource.Dispose();

            throw new MessageBusException("메시지 버스 초기화 중 오류 발생", ex);
        }
    }

    /// <summary>
    /// 옵션으로 기본 전송 계층 생성
    /// </summary>
    private static ITransportLayer CreateDefaultTransportLayer(ITransportOptions options)
    {

        switch (options)
        {
            case SharedMemoryTransportOptions:
                return new SharedMemoryTransportLayer((SharedMemoryTransportOptions)options);
            case UdpTransportOptions:
                return new UdpTransportLayer((UdpTransportOptions)options);
            default:
                return new SharedMemoryTransportLayer((SharedMemoryTransportOptions)options);
        }
        //if(options is SharedMemoryTransportOptions sharedMemoryTransportOptions)
        //{       
        //    return new SharedMemoryTransportLayer(sharedMemoryTransportOptions);
        //}

        //else if(options is UdpTransportOptions udpTransportOptions)
        //{
        //    return new UdpTransportLayer(udpTransportOptions);
        //}              
    }

    /// <summary>
    /// 전송 계층 메시지 수신 처리
    /// </summary>
    private void OnTransportMessageReceived(object sender, TransportMessageReceivedEventArgs args)
    {
        try
        {
            // 메시지 역직렬화
            Message message = _serializer.Deserialize<Message>(args.Data);

            // 메시지 큐에 추가
            _messageQueue.Enqueue(message);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"메시지 역직렬화 오류: {ex.Message}");
        }
    }

    /// <summary>
    /// 지정된 토픽에 메시지 발행
    /// </summary>
    /// <param name="topic">메시지 토픽</param>
    /// <param name="data">메시지 데이터</param>
    /// <returns>발행된 메시지 ID</returns>
    public string Publish(string topic, object data)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MessageBus));

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
        if (_disposed)
            throw new ObjectDisposedException(nameof(MessageBus));

        if (string.IsNullOrEmpty(topic))
            throw new ArgumentNullException(nameof(topic));
        if (string.IsNullOrEmpty(messageId))
            throw new ArgumentNullException(nameof(messageId));

        var message = new Message(topic, data, messageId);
        PublishMessage(message);
    }

    /// <summary>
    /// 메시지 객체 발행
    /// </summary>
    private void PublishMessage(Message message)
    {
        try
        {
            // 메시지 직렬화
            byte[] messageBytes = _serializer.Serialize(message);

            // 전송 계층을 통해 메시지 전송
            _transportLayer.SendMessage(messageBytes);
        }
        catch (Exception ex)
        {
            throw new MessageBusException("메시지 발행 중 오류 발생", ex);
        }
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

        _subscriptions.AddOrUpdate(
            topic,
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
    /// 메시지 처리 루프
    /// </summary>
    private void ProcessMessages()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                // 큐에 있는 메시지 처리
                ProcessQueuedMessages();

                // CPU 사용량 절약
                Thread.Sleep(1);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"메시지 처리 루프 오류: {ex.Message}");
                Thread.Sleep(100); // 연속적인 오류 방지
            }
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
                        Task.WaitAny(new[] { _processingTask }, 1000);
                    }
                }
                catch { }

                // 전송 계층 정리
                if (_transportLayer != null)
                {
                    _transportLayer.MessageReceived -= OnTransportMessageReceived;
                    _transportLayer.Dispose();
                }

                _cancellationTokenSource.Dispose();
            }

            _disposed = true;
        }
    }

    ~MessageBus()
    {
        Dispose(false);
    }
}