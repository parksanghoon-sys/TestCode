namespace MessageBusLib;

/// <summary>
/// 전송 계층 팩토리 인터페이스
/// </summary>
public interface ITransportLayerFactory
{
    /// <summary>
    /// 공유 메모리 전송 계층 생성
    /// </summary>
    /// <param name="busName">버스 이름</param>
    /// <returns>공유 메모리 전송 계층</returns>
    ITransportLayer CreateSharedMemoryTransport(string busName = "default");

    /// <summary>
    /// UDP 전송 계층 생성
    /// </summary>
    /// <param name="multicastIp">멀티캐스트 IP</param>
    /// <param name="port">포트</param>
    /// <returns>UDP 전송 계층</returns>
    ITransportLayer CreateUdpTransport(string multicastIp = "239.0.0.1", int port = 11000);
}
