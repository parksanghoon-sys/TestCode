using BytePacketSupport.Extentions;
using System.Buffers;

namespace BytePacketSupport.BytePacketSupport.Services.Checksum
{
    public enum CheckSum8Type
    {
        XOR,
        NMEA,
        Modulo256,
        TwosComplement,
    }
    public class CheckSum8 : ErrorDetection
    {
        private readonly CheckSum8Type _type;
        public CheckSum8(CheckSum8Type type = CheckSum8Type.XOR)
        {
            _type = type;
        }
        public override ReadOnlySpan<byte> Compute(ReadOnlySpan<byte> data)
        {
            byte ret = 0x00;
            if(_type == CheckSum8Type.XOR || _type == CheckSum8Type.NMEA)            
                ret = Checksum8Xor(data);
            else if(_type == CheckSum8Type.Modulo256)
                ret = Checksum8Modulo256(data);
            else if(_type == CheckSum8Type.TwosComplement)
                ret = Checksum8TwoComplement(data);

            ArrayBufferWriter<byte> retData = new ArrayBufferWriter<byte>();
            using (var span = retData.Reserve(sizeof(byte)))
            {
                span.Span[0] = ret;
            }
            return retData.WrittenSpan;
            
        }

        public override string GetDetectionType()
        {
            return _type.ToString();
        }
        private byte Checksum8Xor(ReadOnlySpan<byte> data)
        {
            byte result = 0;
            foreach (byte b in data)
            {
                result ^= b;
            }
            return result;
        }
        private byte Checksum8Modulo256(ReadOnlySpan<byte> data)
        {
            ulong sum = 0;
            foreach (byte b in data)
                sum += b;
            byte result = (byte)(sum % 256);

            return result;
        }
        private byte Checksum8TwoComplement(ReadOnlySpan<byte> data)
        {
            ulong sum = 0;
            foreach(byte b in data)
                sum += b;
            byte result = (byte)(0x100 - sum);
            return result;
        }
    }
}
