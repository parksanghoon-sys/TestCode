using Protocols.Abstractions.Channels;
using Protocols.Abstractions.Logging;
using Protocols.Modbus.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace Protocols.Modbus.Loggging
{
    public class ModbusExceptionLog : ChannelResponseLog
    {
        private readonly ModbusSerializer _serializer;

        public ModbusExceptionLog(IChannel channel, ModbusExceptionResponse message, byte[] rawMessage, ChannelRequestLog requestLog, ModbusSerializer serializer)
            : base(channel, message, rawMessage, requestLog)
        {
            ExceptionCode = message.ExceptionCode;
            _serializer = serializer;
        }
        public ModbusExceptionCode ExceptionCode { get; }
        /// <summary>
        /// 이 인스턴스의 정규화된 형식 이름을 반환합니다.
        /// </summary>
        /// <returns>정규화된 형식 이름입니다.</returns>
        public override string ToString()
        {
            var stringBuilder = new StringBuilder("RES: ");
            stringBuilder.Append(RawMessage.ModbusRawMessageToString(_serializer));
            stringBuilder.Append(' ');
            var codeName = ExceptionCode.ToString();
            stringBuilder.Append($"Error: {(typeof(ModbusExceptionCode).GetMember(codeName, BindingFlags.Static | BindingFlags.Public)?.FirstOrDefault()?.GetCustomAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute)?.Description ?? codeName}");
            return stringBuilder.ToString();
        }
    }
}
