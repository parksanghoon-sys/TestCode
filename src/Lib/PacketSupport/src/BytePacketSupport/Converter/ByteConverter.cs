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
                    temp = temp.AppendBytes(EEndianChange(byteArray.Skip(0 + i).Take(2).ToArray(), endian));
                }
                byteArray = temp;
            }
            return byteArray;
        }
        public static byte[] GetBytes(short shortByte, EEndian endian = EEndian.LITTLE)
        {
            byte[] bytes = BitConverter.GetBytes(shortByte);

            return GetBytes(bytes, endian);
        }
        public static byte[] GetBytes(int intByte, EEndian endian = EEndian.LITTLE)
        {
            byte[] bytes = BitConverter.GetBytes(intByte);
            return GetBytes(bytes, endian);
        }
        public static byte[] GetBytes(long longBytes, EEndian endian = EEndian.LITTLE)
        {
            byte[] bytes = BitConverter.GetBytes(longBytes);
            return GetBytes(bytes, endian);
        }
        public static byte[] GetBytes(ushort ushortByte, EEndian endian = EEndian.LITTLE)
        {
            byte[] bytes = BitConverter.GetBytes(ushortByte);
            return GetBytes(bytes, endian);
        }

        public static byte[] GetBytes(uint uintByte, EEndian endian = EEndian.LITTLE)
        {
            byte[] bytes = BitConverter.GetBytes(uintByte);
            return GetBytes(bytes, endian);
        }

        public static byte[] GetBytes(ulong ulongBytes, EEndian endian = EEndian.LITTLE)
        {
            byte[] bytes = BitConverter.GetBytes(ulongBytes);
            return GetBytes(bytes, endian);
        }
        private static byte[] EEndianChange(byte[] byteArray, EEndian endian) 
        {
            if (endian == EEndian.BIG || endian == EEndian.LITTLE)
            {
                bool isLittleEEndian = endian == EEndian.LITTLE;
                if (BitConverter.IsLittleEndian != isLittleEEndian)
                    Array.Reverse(byteArray);
            }
            else
            {
                Array.Reverse(byteArray);
            }

            return byteArray;
        }
    }
}
