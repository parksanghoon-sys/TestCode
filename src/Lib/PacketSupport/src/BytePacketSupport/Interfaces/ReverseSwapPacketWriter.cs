using BytePacketSupport.Extentions;
using System.Runtime.InteropServices;

namespace BytePacketSupport.Interfaces
{
    public class ReverseSwapPacketWriter : IPacketWriter
    {
        public static ReverseSwapPacketWriter Instance { get; } = new ReverseSwapPacketWriter();

        public void @int(ReservedSpan span, int value)
        {
            value = ReverseSwap(value);
            MemoryMarshal.Write(span, ref value);
        }

        public void @long(ReservedSpan span, long value)
        {
            value = ReverseSwap(value);
            MemoryMarshal.Write(span, ref value);
        }

        public void @short(ReservedSpan span, short value)
        {
            value = ReverseSwap(value);
            MemoryMarshal.Write(span, ref value);
        }

        public void @uint(ReservedSpan span, uint value)
        {
            value = ReverseSwap(value);
            MemoryMarshal.Write(span, ref value);
        }

        public void @ulong(ReservedSpan span, ulong value)
        {
            value = ReverseSwap(value);
            MemoryMarshal.Write(span, ref value);
        }

        public void @ushort(ReservedSpan span, ushort value)
        {
            value = ReverseSwap(value);
            MemoryMarshal.Write(span, ref value);
        }
        private short ReverseSwap(short value) => value;
        private ushort ReverseSwap(ushort value) => value;
        private int ReverseSwap(int value) => (value << 16) | (value >> 16);
        private uint ReverseSwap(uint value) => (value << 16) | (value >> 16);
        private long ReverseSwap(long value) => (unchecked(((value & (long)0xFF00FF00FF00FF00) >> 8) + ((value & 0x00FF00FF00FF00FF) << 8)));
        private ulong ReverseSwap(ulong value) => (unchecked(((value & (ulong)0xFF00FF00FF00FF00) >> 8) + ((value & 0x00FF00FF00FF00FF) << 8)));

    }
}
