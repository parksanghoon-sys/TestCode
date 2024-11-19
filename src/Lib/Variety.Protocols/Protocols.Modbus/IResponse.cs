namespace Protocols.Modbus
{
    /// <summary>
    /// 프로토콜 응답 메시지
    /// </summary>
    public interface IResponse : IProtocolMessage
    {
    }
    public interface IResponse<TErrorCode> : IResponse where TErrorCode : Enum
    {

    }
}
