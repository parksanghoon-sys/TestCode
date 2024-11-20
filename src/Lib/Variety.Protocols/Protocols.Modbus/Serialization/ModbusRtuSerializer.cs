using Protocols.Modbus.Requests;
using System;
using System.Collections.Generic;
using System.Text;

namespace Protocols.Modbus.Serialization
{
    public sealed class ModbusRtuSerializer : ModbusSerializer
    {
        internal override ModbusResponse DeserializeReadBitResponse(ResponseBuffer buffer, ModbusReadRequest request, int timeout)
        {
            throw new NotImplementedException();
        }

        internal override ModbusResponse DeserializeReadWordResponse(ResponseBuffer buffer, ModbusReadRequest request, int timeout)
        {
            throw new NotImplementedException();
        }

        internal override ModbusRequest DeserializeRequest(RequestBuffer buffer, int timeout)
        {
            throw new NotImplementedException();
        }

        internal override ModbusResponse DeserializeWriteResponse(ResponseBuffer buffer, ModbusWriteCoilRequest request, int timeout)
        {
            throw new NotImplementedException();
        }

        internal override ModbusResponse DeserializeWriteResponse(ResponseBuffer buffer, ModbusWriteHoldingRegisterRequest request, int timeout)
        {
            throw new NotImplementedException();
        }

        internal override IEnumerable<byte> OnSerialize(IModbusMessage message)
        {
            throw new NotImplementedException();
        }
    }
}
