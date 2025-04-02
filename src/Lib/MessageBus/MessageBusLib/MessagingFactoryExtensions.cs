//using MessageBusLib.Pub;
//using MessageBusLib.Sub;

//namespace MessageBusLib;

///// <summary>
///// 메시징 팩토리 확장 메서드
///// </summary>
//public static class MessagingFactoryExtensions
//{
//    /// <summary>
//    /// 메시지 버스와 RPC 서버를 생성하고 서비스 등록
//    /// </summary>
//    public static (IMessageBus, IRpcServer) CreateRpcService(
//        this IMessagingFactory factory,
//        string busName,
//        string serviceTopic,
//        Func<object, Task<object>> handler)
//    {
//        var messageBus = factory.CreateMessageBus(busName);
//        var rpcServer = factory.CreateRpcServer(messageBus, serviceTopic, handler);

//        return (messageBus, rpcServer);
//    }

//    /// <summary>
//    /// 메시지 버스와 RPC 클라이언트 생성
//    /// </summary>
//    public static (IMessageBus, IRpcClient) CreateRpcClient(
//        this IMessagingFactory factory,
//        string busName,
//        int timeout = 30000)
//    {
//        var messageBus = factory.CreateMessageBus(busName);
//        var rpcClient = factory.CreateRpcClient(messageBus, timeout);

//        return (messageBus, rpcClient);
//    }
//}