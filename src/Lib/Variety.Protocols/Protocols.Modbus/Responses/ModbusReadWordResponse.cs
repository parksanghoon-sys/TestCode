using Protocols.Modbus.Requests;

namespace Protocols.Modbus.Responses
{
    public class ModbusReadWordResponse : ModbusReadResponse
    {
        private IReadOnlyList<ushort> values;
        /// <summary>
        /// 응답 Word(Holding Register, Input Register)들의 Raw Byte 배열
        /// </summary>
        public IReadOnlyList<byte> Bytes { get; }

        /// <summary>
        /// 응답 Word(Holding Register, Input Register) 배열
        /// </summary>
        public IReadOnlyList<ushort> Values
        {
            get
            {
                if (values == null)
                {
                    var bytes = Bytes;
                    values = Enumerable.Range(0, bytes.Count / 2).Select(i => (ushort)(bytes[i * 2] << 8 | bytes[i * 2 + 1])).ToArray();
                }
                return values;
            }
        }    
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="bytes">응답할 Word(Holding Register, Input Register)들의 Raw Byte 배열</param>
        /// <param name="request">Modbus 읽기 요청</param>
        internal ModbusReadWordResponse(byte[] bytes, ModbusReadRequest request) : base(request)
        {
            switch (request.Function)
            {
                case ModbusFunction.ReadHoldingRegisters:
                case ModbusFunction.ReadInputRegisters:
                    break;
                default:
                    throw new ArgumentException("The Function in the request does not match.", nameof(request));
            }

            Bytes = bytes ?? throw new ArgumentException(nameof(bytes));
        }
        /// <summary>
        /// 직렬화
        /// </summary>
        /// <returns>직렬화 된 Byte 열거</returns>
        public override IEnumerable<byte> Serialize()
        {
            yield return Request.SlaveAddress;
            yield return (byte)Request.Function;
            yield return (byte)(Request.Length * 2);

            for (int i = 0; i < Request.Length * 2; i++)
            {
                if (i < Bytes.Count)
                    yield return Bytes[i];
                else
                    yield return 0;
            }
        }

        /// <summary>
        /// Modbus 메시지 카테고리
        /// </summary>
        public override ModbusMessageCategory MessageCategory
        {
            get => Request.ObjectType == ModbusObjectType.HoldingRegister
                ? ModbusMessageCategory.ResponseReadHoldingRegister
                : ModbusMessageCategory.ResponseReadInputRegister;
        }
        private IEnumerable<byte> GetRawData(ushort address, int rawDataCount)
        {
            return Bytes.Skip((address - Request.Address) * 2).Take(rawDataCount);
        }

        /// <summary>
        /// 특정 주소로부터 부호 있는 2 Byte 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <returns>값</returns>
        public short GetInt16(ushort address) => GetInt16(address, true);
        /// <summary>
        /// 특정 주소로부터 부호 없는 2 Byte 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <returns>값</returns>
        public ushort GetUInt16(ushort address) => GetUInt16(address, true);
        /// <summary>
        /// 특정 주소로부터 부호 있는 4 Byte 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <returns>값</returns>
        public int GetInt32(ushort address) => GetInt32(address, ModbusEndian.AllBig);
        /// <summary>
        /// 특정 주소로부터 부호 없는 4 Byte 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <returns>값</returns>
        public uint GetUInt32(ushort address) => GetUInt32(address, ModbusEndian.AllBig);
        /// <summary>
        /// 특정 주소로부터 부호 있는 8 Byte 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <returns>값</returns>
        public long GetInt64(ushort address) => GetInt64(address, ModbusEndian.AllBig);
        /// <summary>
        /// 특정 주소로부터 부호 없는 8 Byte 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <returns>값</returns>
        public ulong GetUInt64(ushort address) => GetUInt64(address, ModbusEndian.AllBig);
        /// <summary>
        /// 특정 주소로부터 4 Byte 실수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <returns>값</returns>
        public float GetSingle(ushort address) => GetSingle(address, ModbusEndian.AllBig);
        /// <summary>
        /// 특정 주소로부터 8 Byte 실수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <returns>값</returns>
        public double GetDouble(ushort address) => GetDouble(address, ModbusEndian.AllBig);

        /// <summary>
        /// 특정 주소로부터 부호 있는 2 Byte 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="isBigEndian">빅 엔디안 여부</param>
        /// <returns>값</returns>
        public short GetInt16(ushort address, bool isBigEndian) => BitConverter.ToInt16((isBigEndian ? ModbusEndian.AllBig : ModbusEndian.AllLittle).Sort(GetRawData(address, 2).ToArray()), 0);
        /// <summary>
        /// 특정 주소로부터 부호 없는 2 Byte 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="isBigEndian">빅 엔디안 여부</param>
        /// <returns>값</returns>
        public ushort GetUInt16(ushort address, bool isBigEndian) => BitConverter.ToUInt16((isBigEndian ? ModbusEndian.AllBig : ModbusEndian.AllLittle).Sort(GetRawData(address, 2).ToArray()), 0);
        /// <summary>
        /// 특정 주소로부터 부호 있는 4 Byte 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>값</returns>
        public int GetInt32(ushort address, ModbusEndian endian) => BitConverter.ToInt32(endian.Sort(GetRawData(address, 4).ToArray()), 0);
        /// <summary>
        /// 특정 주소로부터 부호 없는 4 Byte 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>값</returns>
        public uint GetUInt32(ushort address, ModbusEndian endian) => BitConverter.ToUInt32(endian.Sort(GetRawData(address, 4).ToArray()), 0);
        /// <summary>
        /// 특정 주소로부터 부호 있는 8 Byte 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>값</returns>
        public long GetInt64(ushort address, ModbusEndian endian) => BitConverter.ToInt64(endian.Sort(GetRawData(address, 8).ToArray()), 0);
        /// <summary>
        /// 특정 주소로부터 부호 없는 8 Byte 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>값</returns>
        public ulong GetUInt64(ushort address, ModbusEndian endian) => BitConverter.ToUInt64(endian.Sort(GetRawData(address, 8).ToArray()), 0);
        /// <summary>
        /// 특정 주소로부터 8 Byte 실수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>값</returns>
        public float GetSingle(ushort address, ModbusEndian endian) => BitConverter.ToSingle(endian.Sort(GetRawData(address, 4).ToArray()), 0);
        /// <summary>
        /// 특정 주소로부터 8 Byte 실수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>값</returns>
        public double GetDouble(ushort address, ModbusEndian endian) => BitConverter.ToDouble(endian.Sort(GetRawData(address, 8).ToArray()), 0);

    }
}
