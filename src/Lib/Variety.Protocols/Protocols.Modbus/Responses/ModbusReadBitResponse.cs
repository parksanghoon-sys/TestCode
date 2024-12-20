using Protocols.Modbus.Requests;

namespace Protocols.Modbus.Responses
{
    public class ModbusReadBitResponse : ModbusReadResponse
    {
        /// <summary>
        /// 응답 Bit (Coil, Discrete Input)목록
        /// </summary>
        public IReadOnlyList<bool> Values { get; }
        internal ModbusReadBitResponse(bool[] value, ModbusReadRequest request)
            : base(request)
        {
            switch(request.Function)
            {
                case ModbusFunction.ReadCoils:
                case ModbusFunction.ReadDiscreteInputs:
                    Values = value;
                    break;
                default:
                    throw new ModbusException(ModbusExceptionCode.IllegalFunction);
            }
            Values = value ?? throw new ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// 직렬화
        /// </summary>
        /// <returns>직렬화 된 Byte 열거</returns>
        public override IEnumerable<byte> Serialize()
        {
            yield return Request.SlaveAddress;
            yield return (byte)Request.Function;
            yield return (byte)Math.Ceiling(Values.Count / 8d);

            int value = 0;

            for (int i = 0; i < Values.Count; i++)
            {
                int bitIndex = i % 8;
                value |= (Values[i] ? 1 : 0) << bitIndex;
                if(bitIndex == 7 || i == Values.Count - 1)\
                        yield return (byte)value;
            }         
        }
        /// <summary>
        /// Modbus 메시지 카테고리
        /// </summary>
        public override ModbusMessageCategory MessageCategory
        {
            get => Request.ObjectType == ModbusObjectType.Coil
                ? ModbusMessageCategory.ResponseReadCoil
                : ModbusMessageCategory.ResponseReadDiscreteInput;
        }
    }
}
