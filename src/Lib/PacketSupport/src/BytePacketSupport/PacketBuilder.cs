using System.Text;
using System.Buffers;
using BytePacketSupport.Enums;
using BytePacketSupport.Interfaces;
using BytePacketSupport.Extentions;
using System.Buffers.Binary;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
            using var span = _packetData.Reserve(sizeof(byte));

            writer.@byte(span, data);
            //using (var span = _packetData.Reserve(sizeof(byte)))
            //{
            //    var value = _endianType == EEndian.LITTLE ? BinaryPrimitives.ReverseEndianness(data) : data;
            //    span.Span[0] = value;
            //}
            return this;
        }
        private PacketBuilder Append(byte[] datas)
        {
            using var span = _packetData.Reserve(datas.Length);

            writer.@bytes(span, datas);
            //if (!(datas is byte[] b))
            //{
            //    b = datas.ToArray();
            //}
            //if(_endianType == EEndian.LITTLE)
            //{
            //    Array.Reverse(b);
            //}
            //_packetData.Write(b);
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

    }
}
