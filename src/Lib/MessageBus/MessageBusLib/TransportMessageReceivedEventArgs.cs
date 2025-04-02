namespace MessageBusLib;

/// <summary>
/// 전송 계층 메시지 수신 이벤트 인자
/// </summary>
public class TransportMessageReceivedEventArgs : EventArgs
{
    /// <summary>
    /// 수신된 메시지 데이터
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// 발신 엔드포인트 (선택적)
    /// </summary>
    public object SenderEndpoint { get; }

    /// <summary>
    /// 이벤트 인자 초기화
    /// </summary>
    /// <param name="data">수신된 데이터</param>
    /// <param name="senderEndpoint">발신 엔드포인트 (선택적)</param>
    public TransportMessageReceivedEventArgs(byte[] data, object senderEndpoint = null)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        SenderEndpoint = senderEndpoint;
    }
}
