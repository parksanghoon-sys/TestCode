using BytePacketSupport.Extentions;
using System.Runtime.InteropServices;

namespace BytePacketSupport.Interfaces
{
    public class ReverseSwapPacketWriter : IPacketWriter
    {
        public static ReverseSwapPacketWriter Instance { get; } = new ReverseSwapPacketWriter();

        public void @byte(ReservedSpan span, byte value)
        {
            MemoryMarshal.Write(span, ref value);
        }

        public void @bytes(ReservedSpan span, byte[] values)
        {
            for (int i = 0; i < values.Length; i += 2)
            {
                if (i + 3 > values.Length)
                {
                    MemoryMarshal.Write(span, ref values[i + 3]);
                    break;
                }
                ushort value = ReverseSwap(BitConverter.ToUInt16(values, i));
                MemoryMarshal.Write(span.Span.Slice(i, 2), ref value);
            }
        }

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
        private long ReverseSwap(long value) => ((long)RotateLeft((uint)value, 16) << 32) + RotateLeft((uint)(value >> 32), 16);
        private ulong ReverseSwap(ulong value) => ((ulong) RotateLeft ((uint) value, 16) << 32) + RotateLeft((uint)(value >> 32), 16);
        public uint RotateLeft(uint value, int offset)
  => (value << offset) | (value >> (32 - offset));
    }
}
