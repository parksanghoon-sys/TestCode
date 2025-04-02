namespace MessageBusLib.Exceptions;

/// <summary>
/// RPC 예외
/// </summary>
public class RpcException : MessageBusException
{
    public RpcException(string message) : base(message) { }
    public RpcException(string message, Exception innerException) : base(message, innerException) { }
}
