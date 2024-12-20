using Protocols.Abstractions.Channels;
using Protocols.Modbus.Serialization;
using Protocols.Abstractions.Logging;
using Protocols.Modbus.Responses;

namespace Protocols.Modbus.Loggging
{
    /// <summary>
    /// 통신 채널을 통해 주고 받은 Modbus 응답 메시지에 대한 Log
    /// </summary>
    public class ModbusResponseLog : ChannelResponseLog
    {        
        private readonly ModbusSerializer _serializer;

        public ModbusResponseLog(IChannel channel, ModbusResponse response, byte[] rawMessage, ModbusRequestLog requestLog, ModbusSerializer serializer)
            :base(channel, response, rawMessage, requestLog) 
        {
            ModbusResponse = response;
            _serializer = serializer;
        }
        /// <summary>
        /// Modbus 응답 메시지
        /// </summary>
        public ModbusResponse ModbusResponse { get; }

        /// <summary>
        /// 이 인스턴스의 정규화된 형식 이름을 반환합니다.
        /// </summary>
        /// <returns>정규화된 형식 이름입니다.</returns>
        public override string ToString() => $"RES: {RawMessage.ModbusRawMessageToString(_serializer)}";
    }
}
