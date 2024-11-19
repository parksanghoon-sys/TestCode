namespace Protocols.Modbus.Requests
{
    /// <summary>
    /// 프로토콜 요청 메시지
    /// </summary>
    public interface IRequest : IProtocolMessage
    {
    }
    /// <summary>
    /// 플토콜 요청 메시지
    /// </summary>
    /// <typeparam name="TErrorCode">프로토콜 요청 발생시 발생 오류 코드 형식</typeparam>
    public interface IRequest<TErrorCode> : IRequest where TErrorCode : Enum
    {

    }
}
