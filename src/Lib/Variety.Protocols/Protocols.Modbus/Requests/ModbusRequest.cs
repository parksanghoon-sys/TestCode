using Protocols.Modbus.Loggging;

namespace Protocols.Modbus.Requests
{
    /// <summary>
    /// Modbus 요청
    /// </summary>
    public abstract class ModbusRequest : IModbusMessage, IRequest
    {
        /// <summary>
        /// Modbus Object 형식
        /// </summary>
        public abstract ModbusObjectType ObjectType { get; }
        /// <summary>
        /// 슬레이브 주소
        /// </summary>
        public byte SlaveAddress { get; set; }
        /// <summary>
        /// Function
        /// </summary>
        public ModbusFunction Function { get; }
        /// <summary>
        /// 데이터 주소
        /// </summary>
        public ushort Address { get; set; }
        /// <summary>
        /// 요청 길이
        /// </summary>
        public abstract ushort Length { get; }

        /// <summary>
        /// 직렬화
        /// </summary>
        /// <returns>직렬화 결과 Byte 열거</returns>
        public abstract IEnumerable<byte> Serialize();
        /// <summary>
        /// 트랜잭션 ID (Modbus TCP에서 사용), null일 경우 ModbusTcpSerializer가 자동 생성한 ID를 사용하여 요청함. 단, 이 속성은 그대로 null로 유지함.
        /// </summary>
        public ushort? TransactionID { get; set; }

        /// <summary>
        /// Modbus 메시지 카테고리
        /// </summary>
        public abstract ModbusMessageCategory MessageCategory { get; }


        protected ModbusRequest(byte slaveAddress, ModbusFunction function, ushort address)
        {
            SlaveAddress = slaveAddress;
            Function = function;
            Address = address;
        }
    }
}
