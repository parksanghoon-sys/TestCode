using Protocols.Modbus.Requests;
using Protocols.Modbus.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Protocols.Modbus.Serialization
{
    public sealed class ModbusAsciiSerializer : ModbusSerializer
    {
        private readonly List<byte> errorBuffer = new List<byte>();

        internal override IEnumerable<byte> OnSerialize(IModbusMessage message)
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append(":");

            byte lrc = 0;

            foreach (var b in message.Serialize())
            {
                stringBuilder.AppendFormat("{0:X2}", b);
                lrc += b;
            }

            lrc = (byte)(-lrc & 0xff);
            stringBuilder.AppendFormat("{0:X2}", lrc);
            stringBuilder.Append("\r\n");

            return Encoding.ASCII.GetBytes(stringBuilder.ToString());
        }
        internal override byte Read(ResponseBuffer buffer, int index, int timeout)
        {
            if (index * 2 >= buffer.Count - 2)
                buffer.Read((uint)(index * 2 - buffer.Count + 3), timeout);

            return byte.Parse(Encoding.ASCII.GetString(buffer.Skip(index * 2 + 1).Take(2).ToArray()), System.Globalization.NumberStyles.HexNumber, null);
        }
        internal override IEnumerable<byte> Read(ResponseBuffer buffer, int index, int count, int timeout)
        {
            if ((index + count) * 2 > buffer.Count - 1)
                buffer.Read((uint)((index + count) * 2 - buffer.Count + 1), timeout);

            var bytes = buffer.Skip(index * 2 + 1).Take(count * 2).ToArray();
            for (int i = 0; i < count; i++)
                yield return byte.Parse(Encoding.ASCII.GetString(bytes, i * 2, 2), System.Globalization.NumberStyles.HexNumber, null);
        }
        internal ushort ToUInt16(ResponseBuffer buffer, int index, int timeout)
        {
            return ToUInt16(Read(buffer, index, 2, timeout).ToArray(), 0);
        }
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
    }
}
