using System.Buffers;

namespace BytePacketSupport.Extentions
{
    public static class ArrayBufferWriterExtension
    {
        public static ReservedSpan Reserve(this ArrayBufferWriter<byte> writer, int length)
        {
            return new ReservedSpan(writer, length);
        }
        public static byte[] ToArray(this ArrayBufferWriter<byte> writer) => writer.WrittenSpan.ToArray();
    }
}
