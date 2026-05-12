using CleanProtocolSample.Common;
using System.Threading.Channels;

namespace CleanProtocolSample.Protocol;
/// <summary>
/// 프로토콜 클라이언트
/// request/response/event 처리담당
/// </summary>
internal interface IProtocolClient
{
    ChannelReader<ProtocolMessage> Events { get; }
    IMessageInbox Inbox { get; }
    IMessageRouter Router { get; }
    IRecentEventStore RecentEventStore { get; }
    Task<Result> StartAsync(CancellationToken cancellationToken);
    Task<Result<ProtocolMessage>> RequestAsync(string code, string payload, TimeSpan timeOut, CancellationToken cancellationToken);
}

/// <summary>
/// 프로토콜 중앙 엔진.
///
/// 역할:
/// - request 처리
/// - response 매칭
/// - event 분배
/// - inbox 처리
/// - router 처리
/// - event stream 제공
///
/// 시스템 핵심 orchestration 계층.
/// </summary>
internal sealed class ProtocolClient : IProtocolClient
{
    /// <summary>
    /// 공용 session 객체.
    /// </summary>
    private readonly SessionContext _session;

    /// <summary>
    /// 실제 IO loop 담당 객체.
    /// </summary>
    private readonly SocketSession _socket;

    /// <summary>
    /// pending request 저장소.
    /// </summary>
    private readonly PendingRequestStore _pending;

    /// <summary>
    /// 조건 기반 이벤트 대기 처리.
    /// </summary>
    private readonly MessageInbox _inbox;

    /// <summary>
    /// 이벤트 코드별 분배 처리.
    /// </summary>
    private readonly MessageRouter _router;

    /// <summary>
    /// correlation id 생성기.
    /// </summary>
    private readonly ICorrelationIdGenerator _ids;

    /// <summary>
    /// 전체 이벤트 스트림.
    /// </summary>
    private readonly Channel<ProtocolMessage> _events;
    public IMessageInbox Inbox => _inbox;
    public IMessageRouter Router => _router;
    public IRecentEventStore RecentEvents { get; }
    public ChannelReader<ProtocolMessage> Events => _events.Reader;
    public ProtocolClient(SessionContext session, ICorrelationIdGenerator ids)
    {
        _session = session; 
        _ids = ids;
        _socket = new SocketSession(session);
        _pending = new PendingRequestStore();
        _inbox = new MessageInbox();
        _router = new MessageRouter();
        _events = Channel.CreateUnbounded<ProtocolMessage>();
        RecentEvents = new RecentEventStore();
    }
    /// <summary>
    /// protocol engine 시작.
    ///
    /// 내부 loop:
    /// - send loop
    /// - receive loop
    /// - dispatch loop
    /// </summary>
    public Task<Result> StartAsync(CancellationToken cancellationToken = default)
    {
        _ = Task.Run(() =>  
            _socket.RunSendLoopAsync(cancellationToken),cancellationToken);

        _ = Task.Run(() => 
            _socket.RunReceiveLoopAsync(cancellationToken),
            cancellationToken);

        _ = Task.Run(() => 
            RunDispatchLoopAsync(cancellationToken),
            cancellationToken);

        return Task.FromResult(Result.Success());
    }
    /// <summary>
    /// request 전송 후
    /// response 대기.
    ///
    /// 흐름:
    ///
    /// request 생성
    /// -> pending 등록
    /// -> send queue 전달
    /// -> response 대기
    /// </summary>
    public async Task<Result<ProtocolMessage>> RequestAsnc(string code, string payload, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var id = _ids.Next();

        var request = new ProtocolMessage(id, EMessageKind.Request, EResponseStatus.None, code, payload);

        var pending = _pending.Register(id);

        await _session.Queues.SendQueue.Writer.WriteAsync(request, cancellationToken);

        using var timeoutCts = new CancellationTokenSource(timeout);

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,timeoutCts.Token);

        try
        {
            var response = await pending.WaitAsync(linked.Token);

            return Result<ProtocolMessage>.Success(response);
        }
        catch (OperationCanceledException)
        {

            return Result<ProtocolMessage>.Fail(ErrorCodes.Timeout, "Request timeout");
        }
    }
    /// <summary>
    /// receive queue 소비 loop.
    ///
    /// 역할:
    /// - response complete
    /// - event publish
    /// - inbox 전달
    /// - router 전달
    /// - event stream 전달
    /// </summary>
    private async Task RunDispatchLoopAsync(CancellationToken cancellationToken)
    {
        await foreach (var message in _session.Queues
                                                .ReceiveQueue
                                                .Reader
                                                .ReadAllAsync(cancellationToken))
        {
            switch(message.Kind)
            {
                case EMessageKind.Response:
                    _pending.TryComplete(message); 
                    break;
                case EMessageKind.Event:
                    RecentEvents.Add(message);

                    await _events.Writer.WriteAsync(message, cancellationToken);
                    await _router.PublishAsync(message, cancellationToken);
                    await _inbox.PublishAsync(message, cancellationToken);
                    break;
            }
        }
    }
}