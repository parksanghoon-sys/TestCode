using Protocols.Modbus.Requests;
using System;
using System.Collections.Generic;
using System.Text;

namespace Protocols.Modbus
{
    /// <summary>
    /// Modbus 정답 응답
    /// </summary>
    public abstract class ModbusOkResponse : ModbusResponse
    {
        internal ModbusOkResponse(ModbusRequest request) : base(request) { }
    }
    /// <summary>
    /// Modbus Exception 응답
    /// </summary>
    public class ModbusExceptionResponse : ModbusOkResponse
    {
        private readonly ModbusExceptionCode _exceptionCode;

        internal ModbusExceptionResponse(ModbusExceptionCode exceptionCode, ModbusRequest request) :base(request)
        {            
            _exceptionCode = exceptionCode;
        }
        /// <summary>
        /// 직렬화
        /// </summary>
        /// <returns>직렬화 된 Byte 열거</returns>
        public override IEnumerable<byte> Serialize()
        {
            yield return Request.SlaveAddress;
            yield return (byte)((int)Request.Function | 0x80);
            yield return (byte)_exceptionCode;
        }
        /// <summary>
        /// Modbus 메시지 카테고리
        /// </summary>
        public override ModbusMessageCategory MessageCategory { get => ModbusMessageCategory.ResponseException; }
    }

}
