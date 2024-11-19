namespace Protocols.Modbus
{
    public interface IModbusMessage : IProtocolMessage
    {
        /// <summary>
        /// 트랜잭선 ID 
        /// </summary>
        ushort? TransactionID { get; }
        /// <summary>
        /// Modbus 메시지 카테고리
        /// </summary>
        ModbusMessageCategory MessageCategory { get; }
        /// <summary>
        /// 직렬화
        /// </summary>
        /// <returns>직렬화 된 Byte 열거</returns>
        IEnumerable<byte> Serialize();
    }
}
