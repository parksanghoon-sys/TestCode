using Protocols.Abstractions;
using Protocols.Modbus.Requests;

namespace Protocols.Modbus
{
    /// <summary>
    /// Modbus 응답
    /// </summary>
    public abstract class ModbusResponse : IModbusMessage, IResponse
    {
        private ushort? transactionID = null;
        public ModbusRequest Request { get; private set; }
        /// <summary>
        /// 직렬화
        /// </summary>
        /// <returns>직렬화 된 Byte 열거</returns>
        public abstract IEnumerable<byte> Serialize();

        /// <summary>
        /// 트랜잭션 ID (Modbus TCP에서 사용)
        /// </summary>
        public ushort? TransactionID { get => transactionID ?? Request.TransactionID; set => transactionID = value; }
        /// <summary>
        /// Modbus 메시지 카테고리
        /// </summary>
        public abstract ModbusMessageCategory MessageCategory { get; }
        protected ModbusResponse(ModbusRequest  request)
        {
            this.Request = request ?? throw new ArgumentException(nameof(request));
        }
    }
}
