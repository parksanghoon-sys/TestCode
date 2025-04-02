using MessageBusLib.Pub;
using MessageBusLib.Serialization;
using MessageBusLib.Sub;

namespace MessageBusLib;

public interface IMessagingFactory
{
    IMessageBus CreateMessageBus(string busName = "default");
    IMessageBus CreateMessageBus(MessageBusOptions options);
    IRpcClient CreateRpcClient(IMessageBus messageBus, int timeout = 30000);
    IRpcClient CreateRpcClient(IMessageBus messageBus, RpcClientOptions options);
    IRpcServer CreateRpcServer(IMessageBus messageBus, string serviceTopic, Func<object, Task<object>> handler);
    ISerializer CreateSerializer();
}