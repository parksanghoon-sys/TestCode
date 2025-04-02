namespace MessageBusLib;

/// <summary>
/// 추상 전송 계층 기본 구현
/// </summary>
public abstract class TransportLayerBase : ITransportLayer
{
    private bool _disposed = false;
    protected bool IsRunning { get; private set; } = false;
    protected CancellationTokenSource CancellationTokenSource { get; private set; }

    /// <summary>
    /// 메시지 수신 이벤트
    /// </summary>
    private EventHandler<TransportMessageReceivedEventArgs> _messageReceived;    

    // 명시적으로 add/remove 액세서 구현

    event EventHandler<TransportMessageReceivedEventArgs> ITransportLayer.MessageReceived
    {
        add { _messageReceived += value; }
        remove { _messageReceived -= value; }
    }

    /// <summary>
    /// 추상 전송 계층 초기화
    /// </summary>
    protected TransportLayerBase()
    {
        CancellationTokenSource = new CancellationTokenSource();
    }

    /// <summary>
    /// 메시지 전송 (구현 필요)
    /// </summary>
    public abstract void SendMessage(byte[] data);

    /// <summary>
    /// 전송 계층 시작
    /// </summary>
    public virtual void Start()
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);

        if (IsRunning)
            return;

        IsRunning = true;
        OnStart();
    }

    /// <summary>
    /// 전송 계층 중지
    /// </summary>
    public virtual void Stop()
    {
        if (!IsRunning)
            return;

        IsRunning = false;
        CancellationTokenSource.Cancel();
        OnStop();
    }

    /// <summary>
    /// 시작 시 호출되는 메서드 (오버라이드 가능)
    /// </summary>
    protected virtual void OnStart() { }

    /// <summary>
    /// 중지 시 호출되는 메서드 (오버라이드 가능)
    /// </summary>
    protected virtual void OnStop() { }

    /// <summary>
    /// 메시지 수신 이벤트 발생
    /// </summary>
    protected virtual void OnMessageReceived(TransportMessageReceivedEventArgs args)
    {
        _messageReceived?.Invoke(this, args);
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
                Stop();
                CancellationTokenSource.Dispose();
            }

            _disposed = true;
        }
    }

    ~TransportLayerBase()
    {
        Dispose(false);
    }
}
