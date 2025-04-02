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

    /// <summary>
    /// 기본 생성자
    /// </summary>
    public MessagingFactory()
    {
        _serializer = new JsonSerializer();
    }

    /// <summary>
    /// 직렬화 도구 지정 생성자
    /// </summary>
    public MessagingFactory(ISerializer serializer)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    /// <summary>
    /// 메시지 버스 생성
    /// </summary>
    public IMessageBus CreateMessageBus(string busName = "default")
    {
        if (string.IsNullOrEmpty(busName))
            throw new ArgumentNullException(nameof(busName));

        return new MessageBus(new MessageBusOptions { BusName = busName }, _serializer);
    }

    /// <summary>
    /// 메시지 버스 생성 (옵션 지정)
    /// </summary>
    public IMessageBus CreateMessageBus(MessageBusOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        return new MessageBus(options, _serializer);
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
}
