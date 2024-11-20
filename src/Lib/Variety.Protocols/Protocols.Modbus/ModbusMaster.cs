using Protocols.Abstractions.Channels;
using Protocols.Modbus.Requests;
using System;
using System.Collections.Generic;
using System.Text;

namespace Protocols.Modbus
{
    public class ModbusMaster : IDisposable
    {
        private IChannel channel;
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

        public ModbusMaster()
        {
            
        }        
        public ModbusMaster(IChannel channel)
        {
            this.channel = channel;
        }
        /// <summary>
        /// Modubus 요청하기
        /// </summary>
        /// <param name="request">Modbus 요청</param>
        /// <returns>응답</returns>
        public ModbusResponse Requuest(ModbusRequest request) => Request(request, timeout);
        // TODO : ChannelProvider 생성하기
        public ModbusResponse Request(ModbusRequest request, int timeout)
        {
            Channel channel = (Channel as Channel) ?? (Channel as ChannelProvider)?.PrimaryChannel;

            if(channel == null)
                throw new ArgumentNullException(nameof(channel));
            

            return default(ModbusResponse);
        }
        public void Dispose()
        {
            channel?.Dispose();
        }
    }
}
