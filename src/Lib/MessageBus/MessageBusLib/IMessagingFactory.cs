using MessageBusLib.Pub;
using MessageBusLib.Serialization;
using MessageBusLib.Sub;

namespace MessageBusLib;

public interface IMessagingFactory
{
    /// <summary>
    /// 메시지 버스 생성
    /// </summary>
    IMessageBus CreateMessageBus(string busName = "default");

    /// <summary>
    /// 메시지 버스 생성 (옵션 지정)
    /// </summary>
    IMessageBus CreateMessageBus(ITransportOptions options);

    /// <summary>
    /// UDP 전송 계층을 사용하는 메시지 버스 생성
    /// </summary>
    IMessageBus CreateUdpMessageBus(string multicastIp = "239.0.0.1", int port = 11000);

    /// <summary>
    /// 지정된 전송 계층을 사용하는 메시지 버스 생성
    /// </summary>
    IMessageBus CreateMessageBus(ITransportLayer transportLayer);

    /// <summary>
    /// RPC 클라이언트 생성
    /// </summary>
    IRpcClient CreateRpcClient(IMessageBus messageBus, int timeout = 30000);

    /// <summary>
    /// RPC 클라이언트 생성 (옵션 지정)
    /// </summary>
    IRpcClient CreateRpcClient(IMessageBus messageBus, RpcClientOptions options);

    /// <summary>
    /// RPC 서버 생성
    /// </summary>
    IRpcServer CreateRpcServer(IMessageBus messageBus, string serviceTopic, Func<object, Task<object>> handler);

    /// <summary>
    /// 직렬화 도구 생성
    /// </summary>
    ISerializer CreateSerializer();

    /// <summary>
    /// 공유 메모리 전송 계층 생성
    /// </summary>
    ITransportLayer CreateSharedMemoryTransport(string busName = "default");

    /// <summary>
    /// UDP 전송 계층 생성
    /// </summary>
    ITransportLayer CreateUdpTransport(string multicastIp = "239.0.0.1", int port = 11000);
}