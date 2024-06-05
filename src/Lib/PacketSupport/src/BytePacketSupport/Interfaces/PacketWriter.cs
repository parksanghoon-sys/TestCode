using BytePacketSupport.Extentions;
using System.Runtime.InteropServices;

namespace BytePacketSupport.Interfaces
{
    public class PacketWriter : IPacketWriter
    {
        public static PacketWriter Instance { get; } = new PacketWriter();
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
