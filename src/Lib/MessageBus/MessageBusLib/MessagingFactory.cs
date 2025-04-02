using MessageBusLib.Pub;
using MessageBusLib.Serialization;
using MessageBusLib.Sub;

namespace MessageBusLib;

/// <summary>
/// 메시징 컴포넌트 팩토리 구현
/// </summary>
public class MessagingFactory : IMessagingFactory
{
    private readonly ISerializer _serializer;
    private readonly ITransportLayerFactory _transportFactory;

    /// <summary>
    /// 기본 생성자
    /// </summary>
    public MessagingFactory()
    {
        _serializer = new JsonSerializer();
        _transportFactory = new TransportLayerFactory();
    }

    /// <summary>
    /// 직렬화 도구 지정 생성자
    /// </summary>
    public MessagingFactory(ISerializer serializer)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _transportFactory = new TransportLayerFactory();
    }

    /// <summary>
    /// 전체 지정 생성자
    /// </summary>
    public MessagingFactory(ISerializer serializer, ITransportLayerFactory transportFactory)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _transportFactory = transportFactory ?? throw new ArgumentNullException(nameof(transportFactory));
    }

    /// <summary>
    /// 메시지 버스 생성
    /// </summary>
    public IMessageBus CreateMessageBus(string busName = "default")
    {
        if (string.IsNullOrEmpty(busName))
            throw new ArgumentNullException(nameof(busName));

        var options = new SharedMemoryTransportOptions { BusName = busName };
        var transport = _transportFactory.CreateSharedMemoryTransport(busName);

        return new MessageBus(options, _serializer, transport);
    }

    /// <summary>
    /// 메시지 버스 생성 (옵션 지정)
    /// </summary>
    public IMessageBus CreateMessageBus(ITransportOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));
        ITransportLayer transport;
        switch (options)
        {
            case SharedMemoryTransportOptions:
                transport = new SharedMemoryTransportLayer((SharedMemoryTransportOptions)options);
                break;
            case UdpTransportOptions:
                transport = new UdpTransportLayer((UdpTransportOptions)options);
                break;
            default:
                transport = new SharedMemoryTransportLayer((SharedMemoryTransportOptions)options);
                break;
        }
        return new MessageBus(options, _serializer, transport);
    }

    /// <summary>
    /// UDP 전송 계층을 사용하는 메시지 버스 생성
    /// </summary>
    public IMessageBus CreateUdpMessageBus(string multicastIp = "239.0.0.1", int port = 11000)
    {
        var transport = _transportFactory.CreateUdpTransport(multicastIp, port);
        return new MessageBus(new UdpTransportOptions(), _serializer, transport);
    }

    /// <summary>
    /// 지정된 전송 계층을 사용하는 메시지 버스 생성
    /// </summary>
    public IMessageBus CreateMessageBus(ITransportLayer transportLayer)
    {
        if (transportLayer == null)
            throw new ArgumentNullException(nameof(transportLayer));

        return new MessageBus(new SharedMemoryTransportOptions(), _serializer, transportLayer);
    }

    /// <summary>
    /// RPC 클라이언트 생성
    /// </summary>
    public IRpcClient CreateRpcClient(IMessageBus messageBus, int timeout = 30000)
    {
        if (messageBus == null)
            throw new ArgumentNullException(nameof(messageBus));

        return new RpcClient(messageBus, new RpcClientOptions { Timeout = timeout }, _serializer);
    }

    /// <summary>
    /// RPC 클라이언트 생성 (옵션 지정)
    /// </summary>
    public IRpcClient CreateRpcClient(IMessageBus messageBus, RpcClientOptions options)
    {
        if (messageBus == null)
            throw new ArgumentNullException(nameof(messageBus));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        return new RpcClient(messageBus, options, _serializer);
    }

    /// <summary>
    /// RPC 서버 생성
    /// </summary>
    public IRpcServer CreateRpcServer(IMessageBus messageBus, string serviceTopic, Func<object, Task<object>> handler)
    {
        if (messageBus == null)
            throw new ArgumentNullException(nameof(messageBus));
        if (string.IsNullOrEmpty(serviceTopic))
            throw new ArgumentNullException(nameof(serviceTopic));
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        return new RpcServer(messageBus, serviceTopic, handler, _serializer);
    }

    /// <summary>
    /// 직렬화 도구 생성
    /// </summary>
    public ISerializer CreateSerializer()
    {
        return new JsonSerializer();
    }

    /// <summary>
    /// 공유 메모리 전송 계층 생성
    /// </summary>
    public ITransportLayer CreateSharedMemoryTransport(string busName = "default")
    {
        return _transportFactory.CreateSharedMemoryTransport(busName);
    }

    /// <summary>
    /// UDP 전송 계층 생성
    /// </summary>
    public ITransportLayer CreateUdpTransport(string multicastIp = "239.0.0.1", int port = 11000)
    {
        return _transportFactory.CreateUdpTransport(multicastIp, port);
    }
}

/// <summary>
/// 메시징 팩토리 확장 메서드
/// </summary>
public static class MessagingFactoryExtensions
{
    /// <summary>
    /// 메시지 버스와 RPC 서버를 생성하고 서비스 등록
    /// </summary>
    public static (IMessageBus, IRpcServer) CreateRpcService(
        this IMessagingFactory factory,
        string busName,
        string serviceTopic,
        Func<object, Task<object>> handler)
    {
        var messageBus = factory.CreateMessageBus(busName);
        var rpcServer = factory.CreateRpcServer(messageBus, serviceTopic, handler);

        return (messageBus, rpcServer);
    }

    /// <summary>
    /// UDP 기반 메시지 버스와 RPC 서버를 생성하고 서비스 등록
    /// </summary>
    public static (IMessageBus, IRpcServer) CreateUdpRpcService(
        this IMessagingFactory factory,
        string multicastIp,
        int port,
        string serviceTopic,
        Func<object, Task<object>> handler)
    {
        var messageBus = factory.CreateUdpMessageBus(multicastIp, port);
        var rpcServer = factory.CreateRpcServer(messageBus, serviceTopic, handler);

        return (messageBus, rpcServer);
    }

    /// <summary>
    /// 메시지 버스와 RPC 클라이언트 생성
    /// </summary>
    public static (IMessageBus, IRpcClient) CreateRpcClient(
        this IMessagingFactory factory,
        string busName,
        int timeout = 30000)
    {
        var messageBus = factory.CreateMessageBus(busName);
        var rpcClient = factory.CreateRpcClient(messageBus, timeout);

        return (messageBus, rpcClient);
    }

    /// <summary>
    /// UDP 기반 메시지 버스와 RPC 클라이언트 생성
    /// </summary>
    public static (IMessageBus, IRpcClient) CreateUdpRpcClient(
        this IMessagingFactory factory,
        string multicastIp,
        int port,
        int timeout = 30000)
    {
        var messageBus = factory.CreateUdpMessageBus(multicastIp, port);
        var rpcClient = factory.CreateRpcClient(messageBus, timeout);

        return (messageBus, rpcClient);
    }
}