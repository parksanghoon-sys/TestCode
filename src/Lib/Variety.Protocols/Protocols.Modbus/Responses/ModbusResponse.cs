using Protocols.Modbus.Requests;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace Protocols.Modbus.Responses
{
    /// <summary>
    /// Modbus 응답
    /// </summary>
    public abstract class ModbusResponse : IModbusMessage, IResponse
    {
        private ushort? transactionID = null;
        /// <summary>
        /// 트랜잭션 ID (Modbus TCP에서 사용), null일 경우 ModbusTcpSerializer가 자동 생성한 ID를 사용하여 요청함. 단, 이 속성은 그대로 null로 유지함.
        /// </summary>
        public ushort? TransactionID { get => transactionID ?? Request.TransactionID; set => transactionID = value; }
        /// <summary>
        /// Modbus 요청
        /// </summary>
        public ModbusRequest Request { get; }
        /// <summary>
        /// 직렬화
        /// </summary>
        /// <returns>직렬화된 byte 열거</returns>
        public abstract IEnumerable<byte> Serialize();
        /// <summary>
        /// Modbus 메시지 카테고리
        /// </summary>
        public abstract ModbusMessageCategory MessageCategory { get; }

        internal ModbusResponse(ModbusRequest request)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
        }
    }
}
