namespace MessageBusLib;

/// <summary>
/// 전송 계층 기본 옵션 인터페이스
/// </summary>
public interface ITransportOptions
{
    /// <summary>
    /// 최대 메시지 크기 (바이트)
    /// </summary>
    int MaxMessageSize { get; set; }
}
