using BytePacketSupport.Extentions;

namespace BytePacketSupport.Interfaces
{
    public interface IPacketWriter
    {
        public static IPacketWriter LittleEndian => BitConverter.IsLittleEndian ? (IPacketWriter) PacketWriter.Instance : ReversePacketWriter.Instance;
        public static IPacketWriter BigEndian => BitConverter.IsLittleEndian ? (IPacketWriter)ReversePacketWriter.Instance : PacketWriter.Instance;
        public static IPacketWriter LittleEndianSwap => BitConverter.IsLittleEndian ? (IPacketWriter)SwapPacketWriter.Instance : ReverseSwapPacketWriter.Instance;
        public static IPacketWriter BigEndianSwap => BitConverter.IsLittleEndian ? (IPacketWriter)ReverseSwapPacketWriter.Instance : SwapPacketWriter.Instance;
        void @short(ReservedSpan span, short value);
        void @int(ReservedSpan span, int value);
        void @long(ReservedSpan span, long value);
        void @ushort(ReservedSpan span, ushort value);
        void @uint(ReservedSpan span, uint value);
        void @ulong(ReservedSpan span, ulong value);
        void @byte(ReservedSpan span, byte value);
        void @bytes(ReservedSpan span, byte[] values);
    }
}
