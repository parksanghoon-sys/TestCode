using Protocols.Modbus.Loggging;

namespace Protocols.Modbus.Requests
{
    /// <summary>
    /// Modbus Coil 쓰기 요청
    /// </summary>
    public class ModbusWriteCoilRequest : ModbusWriteRequest
    {
        private readonly byte byteLength = 0;
        /// <summary>
        /// 다중 Bit(Coil, Discrete Input) 목록
        /// </summary>
        public List<bool> Values { get; }
        /// <summary>
        /// 단일 bit (Coil, Discrete Input)
        /// </summary>
        public bool SingleBitValue => Values != null && Values.Count > 0 ? Values[0] : throw new ModbusException(ModbusExceptionCode.IllegalDataValue);
        public override ushort Length => (ushort)(Values?.Count ?? throw new ModbusException(ModbusExceptionCode.IllegalDataValue));
        public override ModbusObjectType ObjectType => ModbusObjectType.Coil;
        public ModbusWriteCoilRequest(byte slaveAddress, ushort address)
            : base(slaveAddress, ModbusFunction.WriteMultipleCoils, address)
        {

        }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">Coil값</param>
        public ModbusWriteCoilRequest(byte slaveAddress, ushort address, bool value)
            : base(slaveAddress, ModbusFunction.WriteMultipleCoils, address)
        {
            Values = new List<bool> { value };
        }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="values">Coil 값 목록</param>
        public ModbusWriteCoilRequest(byte slaveAddress, ushort address, IEnumerable<bool> values)
            : base(slaveAddress, ModbusFunction.WriteMultipleCoils, address)
        {
            Values = values as List<bool> ?? values.ToList();
            byteLength = (byte)Math.Ceiling(Length / 8d);
        }
        public override IEnumerable<byte> Serialize()
        {
            yield return SlaveAddress;
            yield return (byte)Function;
            yield return (byte)(Address >> 8 & 0xff);
            yield return (byte)(Address & 0xff);

            switch (Function)
            {
                case ModbusFunction.WriteSingleCoil:
                    yield return SingleBitValue ? (byte)0xff : (byte)0x00;
                    break;

                case ModbusFunction.WriteMultipleCoils:
                    yield return (byte)(Length >> 8 & 0xff);
                    yield return (byte)(Length & 0xff);
                    yield return byteLength;
                    int i = 0;
                    int byteValue = 0;
                    foreach (var bit in Values)
                    {
                        if (bit)
                            byteValue |= 1 << i;
                        i++;
                        if (i > 0)
                        {
                            i = 0;
                            yield return (byte)byteValue;
                            byteValue = 0;
                        }
                    }
                    if (i > 0)
                        yield return (byte)byteValue;
                    break;

                default:
                    break;
            }
        }
        /// <summary>
        /// Modbus 메시지 카테고리
        /// </summary>
        public override ModbusMessageCategory MessageCategory
        {
            get => Function == ModbusFunction.WriteMultipleCoils
                ? ModbusMessageCategory.RequestWriteMultiCoil
                : ModbusMessageCategory.RequestWriteSingleCoil;
        }
    }
}
