using CleanProtocolSample.Logging;
using CleanProtocolSample.Protocol;

namespace CleanProtocolSample.Transprot;
/// <summary>
/// 테스트용 fake transport.
/// </summary>
internal class FakeTransport : ITransport
{
    private readonly Lock _sync = new();

    private readonly Queue<ProtocolMessage> _responses = [];
    private readonly IAppLogger _logger;

    public FakeTransport(IAppLogger logger)
    {
        _logger = logger;
    }
    /// <summary>
    /// fake 응답 반환.
    /// </summary>
    public async Task<ProtocolMessage> ReceiveAsync(CancellationToken cancellationToken)
    {
        while (cancellationToken.IsCancellationRequested == false)
        {
            lock (_sync)
            {
                if (_responses.Count > 0)
                    return _responses.Dequeue();
            }
            await Task.Delay(100, cancellationToken);
        }
        throw new OperationCanceledException();
    }
    /// <summary>
    /// 요청 수신 시 fake 응답 생성.
    /// </summary>
    public Task SendAsync(ProtocolMessage message, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            _logger.Info($"Send {message.Code}");

            switch (message.Code)
            {
                case "HELLO":
                    _responses.Enqueue(new ProtocolMessage(
                        message.CorrelationId,
                        EMessageKind.Response,
                        EResponseStatus.Ack,
                        "HELLO_ACK",
                        "WELCOME"));
                    break;

                case "AUTH":
                    _responses.Enqueue(new ProtocolMessage(
                        message.CorrelationId,
                        EMessageKind.Response,
                        EResponseStatus.Ack,
                        "AUTH_ACK",
                        "TOKEN:ABC123"));

                    _responses.Enqueue(new ProtocolMessage(
                        Guid.NewGuid(),
                        EMessageKind.Event,
                        EResponseStatus.None,
                        "READY",
                        "OK"));
                    break;

                case "GET_DATA":
                    _responses.Enqueue(new ProtocolMessage(
                        message.CorrelationId,
                        EMessageKind.Response,
                        EResponseStatus.Ack,
                        "DATA_ACK",
                        "VALUE=42"));
                    break;
            }
        }
        return Task.CompletedTask;
    }
}
