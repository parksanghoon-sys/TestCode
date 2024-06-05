using System;
using System.Collections.Generic;
using System.Text;
using System.Buffers;
using BytePacketSupport.Enums;
using BytePacketSupport.Interfaces;
using BytePacketSupport.Extentions;
using System.Linq;

namespace BytePacketSupport
{
    public partial class PacketBuilder
    {
        private readonly PacketBuilderConfiguration _configuration;
        private ArrayBufferWriter<byte> _packetData = new ArrayBufferWriter<byte>();

        private readonly EEndian _endianType;
        private readonly IPacketWriter writer;
        public PacketBuilder()
        {
            this._configuration = new PacketBuilderConfiguration();

            _endianType = _configuration.DefaultEndian;

            switch (_endianType)
            {
                case EEndian.BIG:
                    writer = IPacketWriter.BigEndian;
                    break;
                case EEndian.LITTLE:
                    writer = IPacketWriter.LittleEndian;
                    break;
                case EEndian.BIGBYTESWAP:
                    writer = IPacketWriter.BigEndianSwap;
                    break;
                case EEndian.LITTLEBYTESWAP:
                    writer = IPacketWriter.LittleEndianSwap;
                    break;
            }
        }
        public PacketBuilder(PacketBuilderConfiguration configuration)
        {
            this._configuration = configuration;

            _endianType = _configuration.DefaultEndian;

            switch (_endianType)
            {
                case EEndian.BIG:
                    writer = IPacketWriter.BigEndian;
                    break;
                case EEndian.LITTLE:
                    writer = IPacketWriter.LittleEndian;
                    break;
                case EEndian.BIGBYTESWAP:
                    writer = IPacketWriter.BigEndianSwap;
                    break;
                case EEndian.LITTLEBYTESWAP:
                    writer = IPacketWriter.LittleEndianSwap;
                    break;
            }
        }
        private PacketBuilder Append(byte data)
        {
            using (var span = _packetData.Reserve(sizeof(byte)))
            {
                span.Span[0] = data;
            }
            return this;
        }
        public PacketBuilder Append(IEnumerable<byte> datas)
        {
            if(!(datas is byte[] b))
            {
                b = datas.ToArray();
            }
            _packetData.Write(b);
            return this;
        }
        public PacketBuilder Append(string ascil)
        {
            var lengh = Encoding.ASCII.GetByteCount(ascil);
            using var span = _packetData.Reserve(lengh);
            Encoding.ASCII.GetBytes(ascil, span);

            return this;
        }
        private PacketBuilder Append(short value)
        {
            using var span = _packetData.Reserve(sizeof(short));

            writer.@short(span, value);
            return this;
        }
        private PacketBuilder Append(int value)
        {
            using var span = _packetData.Reserve(sizeof(int));

            writer.@int(span, value);

            return this;
        }

        private PacketBuilder Append(long value)
        {
            using var span = _packetData.Reserve(sizeof(long));
            writer.@long(span, value);

            return this;
        }

        private PacketBuilder Append(ushort value)
        {
            using var span = _packetData.Reserve(sizeof(ushort));
            writer.@ushort(span, value);
            return this;
        }

        private PacketBuilder Append(uint value)
        {
            using var span = _packetData.Reserve(sizeof(uint));
            writer.@uint(span, value);
            return this;
        }

        private PacketBuilder Append(ulong value)
        {
            using var span = _packetData.Reserve(sizeof(ulong));
            writer.@ulong(span, value);
            return this;
        }
        private PacketBuilder Append<TSource>(TSource appendClass) where TSource : class
        {
            byte[] datas = PacketParse.Serialize(appendClass);
            this.Append(datas);
            return this;
        }
        public byte[] Build()
        {
            return _packetData.ToArray();
        }
        public PacketBuilder AppendByte(byte value) => Append(value);        
        public PacketBuilder AppendBytes(byte[] value) => Append(value);
        public PacketBuilder AppendString(string value) => Append(value);

        public PacketBuilder AppendShort(short value) => Append(value);
        public PacketBuilder AppendUShort(ushort value) => Append(value);
        public PacketBuilder AppendInt(int value) => Append(value);
        public PacketBuilder AppendUInt(uint value) => Append(value);
        public PacketBuilder AppendLong(long value) => Append(value);
        public PacketBuilder AppendULong(ulong value) => Append(value);
        public PacketBuilder AppendPacketBuilder(PacketBuilder packetBuilder) => AppendBytes(packetBuilder.Build());
        public PacketBuilder AppendClass<TSource>(TSource appendClass) where TSource : class => Append(appendClass);
    }
}
