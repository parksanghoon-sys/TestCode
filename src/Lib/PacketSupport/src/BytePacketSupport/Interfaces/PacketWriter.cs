using BytePacketSupport.Extentions;
using System.Runtime.InteropServices;

namespace BytePacketSupport.Interfaces
{
    public class PacketWriter : IPacketWriter
    {
        public static PacketWriter Instance { get; } = new PacketWriter();

        public void @byte(ReservedSpan span, byte value)
        {
            MemoryMarshal.Write(span, ref value);
        }

        public void @bytes(ReservedSpan span, byte[] values)
        {
            new Span<byte>(values).CopyTo(span);
            values.CopyTo(span);            
        }

        public void @int(ReservedSpan span, int value)
        {
            MemoryMarshal.Write(span, ref value);
        }

        public void @long(ReservedSpan span, long value)
        {
            MemoryMarshal.Write(span, ref value);
        }

        public void @short(ReservedSpan span, short value)
        {
            MemoryMarshal.Write(span, ref value);
        }

        public void @uint(ReservedSpan span, uint value)
        {
            MemoryMarshal.Write(span, ref value);
        }   

        public void @ulong(ReservedSpan span, ulong value)
        {
            MemoryMarshal.Write(span, ref value);
        }

        public void @ushort(ReservedSpan span, ushort value)
        {
            MemoryMarshal.Write(span, ref value);
        }
    }
}
