using Protocols.Modbus.Requests;

namespace Protocols.Modbus.Responses
{
    public abstract class ModbusOkResponse : ModbusResponse
    {
        internal ModbusOkResponse(ModbusRequest request)
            : base(request)
        {
        }
    }
    /// <summary>
    /// 모드버스 정상 응답
    /// </summary>
    /// <typeparam name="TResult">Modbus 요청</typeparam>
    public abstract class ModbusOkResponse<TResult> : ModbusOkResponse where TResult : ModbusRequest
    {
        internal ModbusOkResponse(TResult request)
            : base(request)
        {
        }
    }

}
