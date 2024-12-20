using Protocols.Modbus.Requests;

namespace Protocols.Modbus.Responses
{
    /// <summary>
    /// Modbus 정상 응답
    /// </summary>
    public abstract class ModbusReadResponse : ModbusOkResponse<ModbusReadRequest>
    {
        internal ModbusReadResponse(ModbusReadRequest request)
          : base(request)
        {
        }
    }
}
