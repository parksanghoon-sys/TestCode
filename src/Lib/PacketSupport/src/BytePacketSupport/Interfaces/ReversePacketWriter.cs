using BytePacketSupport.Extentions;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace BytePacketSupport.Interfaces
{
    public class ReversePacketWriter : IPacketWriter
    {
        public static ReversePacketWriter Instance { get; } = new ReversePacketWriter();

        public void @byte(ReservedSpan span, byte value)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
            MemoryMarshal.Write(span, ref value);
        }

        public void @bytes(ReservedSpan span, byte[] values)
        {
            Array.Reverse(values);
            new Span<byte>(values).CopyTo(span);
            values.CopyTo(span);
        }

        public void @int(ReservedSpan span, int value)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
            MemoryMarshal.Write(span, ref value);
        }

        public void @long(ReservedSpan span, long value)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
            MemoryMarshal.Write(span, ref value);
        }

        public void @short(ReservedSpan span, short value)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
            MemoryMarshal.Write(span, ref value);
        }

        public void @uint(ReservedSpan span, uint value)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
            MemoryMarshal.Write(span, ref value);
        }

        public void @ulong(ReservedSpan span, ulong value)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
            MemoryMarshal.Write(span, ref value);
        }

        public void @ushort(ReservedSpan span, ushort value)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
            MemoryMarshal.Write(span, ref value);
        }     
    }
}
