namespace BytePacketSupport
{
    public partial class PacketBuilder
	{
        public PacketBuilder AppendByte(byte value) => Append(value);
        public PacketBuilder AppendBytes(byte[] value) => Append(value);
        public PacketBuilder AppendString(string value) => Append(value);

        public PacketBuilder AppendShort(short value) => Append(value);
        public PacketBuilder AppendUShort(ushort value) => Append(value);
        public PacketBuilder AppendInt(int value) => Append(value);
        public PacketBuilder AppendInt16(short value) => Append(value);
        public PacketBuilder AppendUInt(uint value) => Append(value);
        public PacketBuilder AppendLong(long value) => Append(value);
        public PacketBuilder AppendULong(ulong value) => Append(value);
        public PacketBuilder AppendPacketBuilder(PacketBuilder packetBuilder) => AppendBytes(packetBuilder.Build());
        public PacketBuilder AppendClass<TSource>(TSource appendClass) where TSource : class => Append(appendClass);

    }
}
