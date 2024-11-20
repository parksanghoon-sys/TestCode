using Protocols.Abstractions.Channels;
using Protocols.Modbus.Loggging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Protocols.Modbus.Serialization
{
    class ChannelBuffer : List<byte>
    {
        public IChannel Channel { get; }
        internal ChannelBuffer(IChannel channel)
        {
            this.Channel = channel;
        }
        public byte Read(int timeout)
        {
            var result = Channel.Read(timeout);
            Add(result);
            return result;
        }
        public byte[] Read(uint count, int timeout)
        {
            var result = Channel.Read(count,timeout).ToArray();
            AddRange(result);
            return result;
        }
    }
    class RequestBuffer : ChannelBuffer
    {
        internal RequestBuffer(ModbusSlaveService modbusSlave, Channel channel) : base(channel)
        {
            ModbusSlave = modbusSlave;
        }

        public ModbusSlaveService ModbusSlave { get; }
    }

    internal class ModbusSlaveService
    {
    }

    class ResponseBuffer : ChannelBuffer
    {
        internal ResponseBuffer(Channel channel) : base(channel)
        {
        }

        public ModbusRequestLog RequestLog { get; set; }
    }
}
