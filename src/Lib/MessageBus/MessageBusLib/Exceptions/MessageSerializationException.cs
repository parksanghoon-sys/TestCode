namespace MessageBusLib.Exceptions;

/// <summary>
/// 메시지 직렬화 예외
/// </summary>
public class MessageSerializationException : MessageBusException
{
    public MessageSerializationException(string message) : base(message) { }
    public MessageSerializationException(string message, Exception innerException) : base(message, innerException) { }
}
