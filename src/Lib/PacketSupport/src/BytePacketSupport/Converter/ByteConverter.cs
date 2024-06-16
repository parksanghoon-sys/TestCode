using BytePacketSupport.Enums;
using System.Text;

namespace BytePacketSupport.BytePacketSupport.Converter
{
    public static class ByteConverter
    {
        public static byte[] GetBytes(string str) => Encoding.ASCII.GetBytes(str);
        public static byte[] GetBytes(byte[]byteArray, EEndian endian)
        {
            bool isByteArray = endian == EEndian.LITTLE || endian == EEndian.LITTLEBYTESWAP;
            if(BitConverter.IsLittleEndian != isByteArray)
            {
                Array.Reverse(byteArray);
            }

            if(endian == EEndian.LITTLEBYTESWAP || endian == EEndian.BIGBYTESWAP)
            {
                byte[] temp = new byte[0];
                for(int i = 0; i <byteArray.Length; i +=2)
                {
                    temp = temp.Append 
                }
            }
        }
        public static byte[] GetBytes(short shortByte, EEndian endian = EEndian.LITTLE)
        {
            byte[] bytes = BitConverter.GetBytes(shortByte);

            return GetBytes(bytes, endian);
        }

    }
}
