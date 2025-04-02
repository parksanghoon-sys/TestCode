namespace MessageBusLib;

/// <summary>
/// 전송 계층 기본 옵션 추상 클래스
/// </summary>
public abstract class TransportOptionsBase : ITransportOptions
{
    /// <summary>
    /// 최대 메시지 크기 (바이트)
    /// </summary>
    public int MaxMessageSize { get; set; } = 1024 * 1024; // 기본값 1MB
}