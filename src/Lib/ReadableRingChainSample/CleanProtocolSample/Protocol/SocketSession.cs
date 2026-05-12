namespace CleanProtocolSample.Protocol;
/// <summary>
/// 실제 IO 송수신 loop 담당.
///
/// 역할:
/// - send loop
/// - receive loop
///
/// business logic 없음.
///
/// 오직 transport IO만 담당.
/// </summary>
internal sealed class SocketSession
{
    private readonly SessionContext _session;
    public SocketSession(SessionContext session)
    {
        _session = session;        
    }
    /// <summary>
    /// 송신 loop.
    ///
    /// SendQueue를 소비하여
    /// 실제 transport로 전송.
    /// </summary>
    public async Task RunSendLoopAsync(CancellationToken cancellationToken)
    {
        await foreach (var message in _session.Queues.SendQueue.Reader.ReadAllAsync(cancellationToken))
        {
            await _session.Transport.SendAsync(message, cancellationToken).ConfigureAwait(false);
            _session.Queues.RecentSent.Add(message);
        }
    }
    /// <summary>
    /// 수신 loop.
    ///
    /// transport에서 읽은 데이터를
    /// ReceiveQueue에 전달.
    /// </summary>
    public async Task RunReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var message = await _session.Transport.ReceiveAsync(cancellationToken).ConfigureAwait(false);

        _session.Queues.RecentReceived.Add(message);

        await _session.Queues.ReceiveQueue.Writer.WriteAsync(message, cancellationToken);
    }
}

