using BytePacketSupport.BytePacketSupport.Services.CRC;
using BytePacketSupport.Extentions;
using System.Buffers;
using System.Buffers.Binary;

namespace BytePacketSupport.BytePacketSupport.Services.CheckSum
{
    public enum CheckSum16Type
    {
        NORNAL
    }
    public class CheckSum16 : ErrorDetection
    {
        private readonly CheckSum16Type _type;
        private readonly bool _isLittleEndian;

        public CheckSum16(CheckSum16Type type = CheckSum16Type.NORNAL, bool isLittleEndian = true)
        {
            _type = type;
            _isLittleEndian = isLittleEndian;
        }
        public override ReadOnlySpan<byte> Compute(ReadOnlySpan<byte> data)
        {
            ushort ret = 0x00;            
            ret = Checksum(data);

            ArrayBufferWriter<byte> retData = new ArrayBufferWriter<byte>();
            using (var span = retData.Reserve(sizeof(ushort)))
            {
                if (_isLittleEndian == true)
                {
                    BinaryPrimitives.WriteUInt16LittleEndian(span, ret);
                }
                else
                {
                    BinaryPrimitives.WriteUInt16BigEndian(span, ret);
                }
            }
            return retData.WrittenSpan;
        }

        private ushort Checksum(ReadOnlySpan<byte> data)
        {
            ushort num = 0;
            for (int i = 0; i < data.Length; i++)
            {
                num += data[i];
            }

            return num;
        }

        public override string GetDetectionType()
        {
            throw new NotImplementedException();
        }
    }
}
