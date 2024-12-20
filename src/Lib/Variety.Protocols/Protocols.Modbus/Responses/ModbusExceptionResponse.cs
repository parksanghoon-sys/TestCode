using Protocols.Modbus.Requests;

namespace Protocols.Modbus.Responses
{
    /// <summary>
    /// Modbus 예외 응답
    /// </summary>
    public class ModbusExceptionResponse : ModbusOkResponse
    {
        public ModbusExceptionCode ExceptionCode { get; }
        public override ModbusMessageCategory MessageCategory => ModbusMessageCategory.ResponseException;
        public ModbusExceptionResponse(ModbusExceptionCode exceptionCode, ModbusRequest request)
            : base(request)
        {
            ExceptionCode = exceptionCode;
        }
        public override IEnumerable<byte> Serialize()
        {
            yield return Request.SlaveAddress;
            yield return (byte)(0x80 | (byte)Request.Function);
            yield return (byte)ExceptionCode;
        }
    }

}
