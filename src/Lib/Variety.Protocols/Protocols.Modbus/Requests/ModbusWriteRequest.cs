namespace Protocols.Modbus.Requests
{
    public abstract class ModbusWriteRequest : ModbusRequest
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="function">Function</param>
        /// <param name="address">데이터 주소</param>
        protected ModbusWriteRequest(byte slaveAddress, ModbusFunction function, ushort address)
            : base(slaveAddress, function, address)
        {

        }
    }
}
