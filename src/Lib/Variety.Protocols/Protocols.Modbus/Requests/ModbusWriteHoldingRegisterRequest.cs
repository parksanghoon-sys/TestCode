using Protocols.Modbus.Loggging;

namespace Protocols.Modbus.Requests
{
    /// <summary>
    /// Modbus Holding Register 쓰기 요청
    /// </summary>
    public class ModbusWriteHoldingRegisterRequest : ModbusWriteRequest
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">Holding Register 값</param>
        public ModbusWriteHoldingRegisterRequest(byte slaveAddress, ushort address, ushort value)
            : base(slaveAddress, ModbusFunction.WriteSingleHoldingRegister, address)
        {
            Bytes = new List<byte> { (byte)(value >> 8 & 0xff), (byte)(value & 0xff) };
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="bytes">Holding Register 값들의 Raw Byte 목록</param>
        public ModbusWriteHoldingRegisterRequest(byte slaveAddress, ushort address, IEnumerable<byte> bytes)
            : base(slaveAddress, ModbusFunction.WriteMultipleHoldingRegisters, address)
        {
            Bytes = bytes as List<byte> ?? bytes.ToList();
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="values">Holding Register 값 목록</param>
        public ModbusWriteHoldingRegisterRequest(byte slaveAddress, ushort address, IEnumerable<ushort> values)
            : base(slaveAddress, ModbusFunction.WriteMultipleHoldingRegisters, address)
        {
            Bytes = values.SelectMany(word => new byte[] { (byte)(word >> 8 & 0xff), (byte)(word & 0xff) }).ToList();
        }

        /// <summary>
        /// 단일 Holding Register 값
        /// </summary>
        public ushort SingleWordValue => Bytes.Count >= 2 ?
            (ushort)(Bytes[0] << 8 | Bytes[1]) : throw new ModbusException(ModbusExceptionCode.IllegalDataValue);
        /// <summary>
        /// Holding Register 값들의 Raw Byte 목록
        /// </summary>
        public List<byte> Bytes { get; }
        /// <summary>
        /// 길이
        /// </summary>
        public override ushort Length => (ushort)Math.Ceiling(Bytes.Count / 2d);

        /// <summary>
        /// Modbus Object 형식
        /// </summary>
        public override ModbusObjectType ObjectType { get => ModbusObjectType.HoldingRegister; }

        /// <summary>
        /// 직렬화
        /// </summary>
        /// <returns>직렬화 결과 Byte 열거</returns>
        public override IEnumerable<byte> Serialize()
        {
            if (Bytes.Count < 2)
                throw new ModbusException(ModbusExceptionCode.IllegalDataValue);

            yield return SlaveAddress;
            yield return (byte)Function;
            yield return (byte)(Address >> 8 & 0xff);
            yield return (byte)(Address & 0xff);

            byte byteLength = (byte)(Math.Ceiling(Bytes.Count / 2d) * 2);

            switch (Function)
            {
                case ModbusFunction.WriteSingleHoldingRegister:
                    yield return Bytes[0];
                    yield return Bytes[1];
                    break;
                case ModbusFunction.WriteMultipleHoldingRegisters:
                    yield return (byte)(Length >> 8 & 0xff);
                    yield return (byte)(Length & 0xff);
                    yield return byteLength;

                    int i = 0;
                    foreach (var b in Bytes)
                    {
                        yield return b;
                        i++;
                    }

                    if (i % 2 == 1)
                        yield return 0;
                    break;
            }
        }

        /// <summary>
        /// Modbus 메시지 카테고리
        /// </summary>
        public override ModbusMessageCategory MessageCategory
        {
            get => Function == ModbusFunction.WriteMultipleHoldingRegisters
                ? ModbusMessageCategory.RequestWriteMultiHoldingRegister
                : ModbusMessageCategory.RequestWriteSingleHoldingRegister;
        }
    }
}
