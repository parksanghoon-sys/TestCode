using BytePacketSupport.Extentions;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace BytePacketSupport.Interfaces
{
    public class SwapPacketWriter : IPacketWriter
    {
        public static SwapPacketWriter Instance { get;  } = new SwapPacketWriter();

        public void @byte(ReservedSpan span, byte value)
        {            
            MemoryMarshal.Write(span, ref value);
        }

        public void @bytes(ReservedSpan span, byte[] values)
        {
            for (int i = 0; i < values.Length; i += 2)
            {
                if(i +  3 > values.Length)
                {                
                    MemoryMarshal.Write(span, ref values[i +3]);
                    break;
                }
                ushort value = Swap(BitConverter.ToUInt16(values, i));
                MemoryMarshal.Write(span.Span.Slice(i, 2), ref value);
            }
        }

        public void @int(ReservedSpan span, int value)
        {
            value = Swap(value);
            MemoryMarshal.Write(span, ref value);
        }

        public void @long(ReservedSpan span, long value)
        {
            value = Swap(value);
            MemoryMarshal.Write(span, ref value);
        }

        public void @short(ReservedSpan span, short value)
        {
            value = Swap(value);
            MemoryMarshal.Write(span, ref value);
        }

        public void @uint(ReservedSpan span, uint value)
        {
            value = Swap(value);
            MemoryMarshal.Write(span, ref value);
        }

        public void @ulong(ReservedSpan span, ulong value)
        {
            value = Swap(value);
            MemoryMarshal.Write(span, ref value);
        }

        public void @ushort(ReservedSpan span, ushort value)
        {
            value = Swap(value);
            MemoryMarshal.Write(span, ref value);
        }
        private short Swap(short value) => BinaryPrimitives.ReverseEndianness(value);        
        private ushort Swap(ushort value) => BinaryPrimitives.ReverseEndianness(value);
        private int Swap(int value) => unchecked(((value & (int)0xFF00FF00) >> 8) + ((value & (int)0x00FF00FF) << 8));
        private uint Swap(uint value) => unchecked(((value & (uint)0xFF00FF00) >> 8) + ((value & (int)0x00FF00FF) << 8));
        private long Swap(long value) => unchecked(((value & (long)0xFF00FF00FF00FF00) >> 8) + ((value & 0x00FF00FF00FF00FF) << 8));
        private ulong Swap(ulong value) => unchecked(((value & (ulong)0xFF00FF00FF00FF00) >> 8) + ((value & 0x00FF00FF00FF00FF) << 8));
    }
}
