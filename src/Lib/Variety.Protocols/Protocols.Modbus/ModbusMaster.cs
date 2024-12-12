using Protocols.Abstractions.Channels;
using Protocols.Abstractions.Logging;
using Protocols.Modbus.Loggging;
using Protocols.Modbus.Requests;
using Protocols.Modbus.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Protocols.Modbus
{
    public class ModbusMaster : IDisposable
    {
        private IChannel channel;
        private ModbusSerializer serializer;
        private int timeout { get; set; } = 1000;
        public bool ThrowsModbusExceptions { get; set; } = true;

        public IChannel Channel
        {
            get => channel;
            set
            {
                if (channel != value)
                {
                    channel = value;
                }
            }
        }
        /// <summary>
        /// Modbus Serializer
        /// </summary>
        public ModbusSerializer Serializer
        {
            get
            {
                if (serializer == null)
                    serializer = new ModbusRtuSerializer();
                return serializer;
            }
            set
            {
                if (serializer != value)
                {
                    serializer = value;
                }
            }
        }

        public ModbusMaster()
        {
            
        }        
        public ModbusMaster(IChannel channel)
        {
            this.channel = channel;
        }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="serializer">Modbus Serializer</param>
        public ModbusMaster(ModbusSerializer serializer)
        {
            this.serializer = serializer;
        }
        /// <summary>
        /// Modubus 요청하기
        /// </summary>
        /// <param name="request">Modbus 요청</param>
        /// <returns>응답</returns>
        public ModbusResponse Requuest(ModbusRequest request) => Request(request, timeout);
        /// <summary>
        /// Modbus 요청하기
        /// </summary>
        /// <param name="request">Modbus 요청</param>
        /// <param name="timeout">응답 제한 시간</param>
        /// <returns>Modbus 응답</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="RequestException{ModbusCommErrorCode}"></exception>
        /// <exception cref="ModbusException"></exception>
        public ModbusResponse Request(ModbusRequest request, int timeout)
        {
            Channel channel = (Channel as Channel) ?? (Channel as ChannelProvider)?.PrimaryChannel;

            if(channel == null)
                throw new ArgumentNullException(nameof(channel));

            var serializer = Serializer;

            if (serializer is null)
                throw new RequestException<ModbusCommErrorCode>(ModbusCommErrorCode.NotDefinedModbusSerializer, new byte[0], request);

            var buffer = new ResponseBuffer(channel);

            ModbusResponse result;
            try
            {
                result = serializer.Deserialize(buffer, request, timeout);
            }
            catch (RequestException<ModbusCommErrorCode> ex)
            {
                channel?.Logger?.Log(new ChannelErrorLog(channel, ex.InnerException ?? ex));
                throw ex.InnerException ?? ex; 
            }
            if (result is ModbusExceptionResponse exceptionResponse)
            {
                channel?.Logger?.Log(new ModbusExceptionLog(channel, exceptionResponse, buffer.ToArray(), buffer.RequestLog, serializer));
                if (ThrowsModbusExceptions)
                    throw new ModbusException(exceptionResponse.ExceptionCode);
            }
            else
                channel?.Logger?.Log(new ModbusResponseLog(channel, result, result is ModbusCommErrorResponse ? null : buffer.ToArray(), buffer.RequestLog, serializer));


            return result;
        }
        public void Dispose()
        {
            channel?.Dispose();
        }
    }
}
