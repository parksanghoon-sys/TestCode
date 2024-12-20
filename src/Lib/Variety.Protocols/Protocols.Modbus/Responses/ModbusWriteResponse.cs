using Protocols.Modbus.Requests;

namespace Protocols.Modbus.Responses
{
    public class ModbusWriteResponse : ModbusOkResponse<ModbusWriteRequest>
    {
        internal ModbusWriteResponse(ModbusWriteRequest request)
            : base(request)
        {
            switch (request.Function)
            {
                case ModbusFunction.WriteMultipleCoils:
                case ModbusFunction.WriteSingleCoil:
                case ModbusFunction.WriteMultipleHoldingRegisters:
                case ModbusFunction.WriteSingleHoldingRegister:
                    break;
                default:
                    throw new ArgumentException("The Function in the request does not match.", nameof(request));
            }
        }
        public override IEnumerable<byte> Serialize()
        {
            yield return Request.SlaveAddress;
            yield return (byte)Request.Function;
            yield return (byte)((Request.Address >> 8) & 0xff);
            yield return (byte)((Request.Address) & 0xff);            
            switch(Request.Function)
            {
                case ModbusFunction.WriteMultipleCoils:
                case ModbusFunction.WriteMultipleHoldingRegisters:
                    yield return (byte)((Request.Length >> 8) & 0xff);
                    yield return (byte)((Request.Length) & 0xff);
                    break;
                case ModbusFunction.WriteSingleCoil:
                    ModbusWriteCoilRequest writeCoilRequest = Request as ModbusWriteCoilRequest;
                    yield return writeCoilRequest.SingleBitValue ? (byte)0xff : (byte)0x00;
                    yield return 0x00;
                    break;
                case ModbusFunction.WriteSingleHoldingRegister:
                    ModbusWriteHoldingRegisterRequest writeHoldingRegisterRequest = Request as ModbusWriteHoldingRegisterRequest;
                    yield return (byte)((writeHoldingRegisterRequest.SingleWordValue >> 8) & 0xff);
                    yield return (byte)((writeHoldingRegisterRequest.SingleWordValue) & 0xff);
                    break;
            }
        }
        /// <summary>
        /// Modbus 메시지 카테고리
        /// </summary>
        public override ModbusMessageCategory MessageCategory
        {
            get
            {
                switch (Request.Function)
                {
                    case ModbusFunction.WriteMultipleCoils:
                        return ModbusMessageCategory.ResponseWriteMultiCoil;
                    case ModbusFunction.WriteSingleCoil:
                        return ModbusMessageCategory.ResponseWriteSingleCoil;
                    case ModbusFunction.WriteMultipleHoldingRegisters:
                        return ModbusMessageCategory.ResponseWriteMultiHoldingRegister;
                    case ModbusFunction.WriteSingleHoldingRegister:
                        return ModbusMessageCategory.ResponseWriteSingleHoldingRegister;
                    default:
                        return ModbusMessageCategory.None;
                }
            }
        }
    }
}
