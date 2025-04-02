namespace MessageBusLib;

/// <summary>
/// UDP 전송 계층 구성 옵션
/// </summary>
public class UdpTransportOptions : ITransportOptions
{
    /// <summary>
    /// 멀티캐스트 IP 주소
    /// </summary>
    public string MulticastIp { get; set; } = "239.0.0.1";

    /// <summary>
    /// 포트 번호
    /// </summary>
    public int Port { get; set; } = 11000;

    /// <summary>
    /// 최대 메시지 크기
    /// </summary>
    public int MaxMessageSize { get; set; } = 65507; // UDP 최대 크기

    /// <summary>
    /// 로컬 네트워크만 사용
    /// </summary>
    public bool LocalNetworkOnly { get; set; } = true;

    /// <summary>
    /// TTL (Time To Live)
    /// </summary>
    public int TimeToLive { get; set; } = 1;
}
