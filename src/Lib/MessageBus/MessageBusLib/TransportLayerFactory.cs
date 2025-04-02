namespace MessageBusLib;

/// <summary>
/// 전송 계층 팩토리 구현
/// </summary>
public class TransportLayerFactory : ITransportLayerFactory
{
    /// <summary>
    /// 공유 메모리 전송 계층 생성
    /// </summary>
    public ITransportLayer CreateSharedMemoryTransport(string busName = "default")
    {
        var options = new SharedMemoryTransportOptions
        {
            BusName = busName
        };

        return new SharedMemoryTransportLayer(options);
    }

    /// <summary>
    /// UDP 전송 계층 생성
    /// </summary>
    public ITransportLayer CreateUdpTransport(string multicastIp = "239.0.0.1", int port = 11000)
    {
        return new UdpTransportLayer(multicastIp, port);
    }
}
