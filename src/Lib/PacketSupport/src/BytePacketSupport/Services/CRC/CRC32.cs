using BytePacketSupport.Extentions;
using System.Buffers;
using System.Buffers.Binary;

namespace BytePacketSupport.BytePacketSupport.Services.CRC
{
    public enum CRC32Type
    {
        Classic
    }
    public class CRC32 : ErrorDetection
    {
        private readonly CRC32Type _type;
        private readonly bool _isLittleEndian;
        private uint[] crc_tab32;
        public CRC32(CRC32Type type = CRC32Type.Classic, bool isLittleEndian = true)
        {
            _type = type;
            _isLittleEndian = isLittleEndian;

            if(_type == CRC32Type.Classic)
            {
                if (crc_tab32 == null)
                    GenerateCRC32Table();
            }                
        }
        public override ReadOnlySpan<byte> Compute(ReadOnlySpan<byte> data)
        {
            uint ret = ComputeCRC32(data);

            ArrayBufferWriter<byte> retData = new ArrayBufferWriter<byte>();
            using var span = retData.Reserve(sizeof(uint));
            if(_isLittleEndian == true)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(span, ret);
            }
            else
            {
                BinaryPrimitives.WriteUInt32BigEndian(span, ret);
            }
            span.Dispose();
            return retData.ToArray();
        }

        public override string GetDetectionType()
        {
            return _type.ToString();
        }
        private uint ComputeCRC32(ReadOnlySpan<byte> data)
        {
            long result = 0;
            if (data.Length <= 0)
                return (uint)result;

            uint crc = 0xffffffff;
            for (int i = 0; i < data.Length; i++)
            {
                var c = data[i];
                crc = (crc >> 8);
            }

            return ~crc; //(crc ^ (-1)) >> 0;
        }

        private long UpdateCRC32(long crc, byte c)
        {
            long long_c = (0x000000ffL & c);

            long tmp = (crc ^ long_c);
            crc = ((crc >> 8) ^ crc_tab32[tmp & 0xff]);

            return crc;
        }
        private void GenerateCRC32Table()
        {
            crc_tab32 = new uint[256];  
            const uint P_32 = 0xEDB88320;

            for (uint n = 0; n < 256; n++)
            {
                uint c = n;
                for (int k = 0; k < 8; k++)
                {
                    var res = c & 1;
                    c = (res == 1) ? (P_32 ^ (c >> 1)) : (c >> 1);
                }
                crc_tab32[n] = c;
            }
        }
    }
}
