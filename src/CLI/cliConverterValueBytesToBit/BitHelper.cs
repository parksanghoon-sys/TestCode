//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace cliConverterValueBytesToBit
//{
//    internal class BitHelper : IValueToByteConverter
//    {
//        public void FillBits(byte[] bytes, int startBit, int bitCount, uint value, bool fillFromStart)
//        {
//            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
//            if (startBit < 0 || startBit >= bytes.Length * 8) throw new ArgumentOutOfRangeException(nameof(startBit));
//            if (bitCount <= 0 || bitCount > 32) throw new ArgumentOutOfRangeException(nameof(bitCount));
//            //if (!fillFromStart && bitCount > startBit + 1) throw new ArgumentException("Cannot fill bits from end if bitCount is larger than startBit + 1");

//            int startByte = startBit / 8;
//            int startBitInByte = startBit % 8;
//            int endByte = (startBit + bitCount - 1) / 8;

//            if (fillFromStart)
//            {
//                for (int i = startByte; i <= endByte; i++)
//                {
//                    int bitsInThisByte = Math.Min(8 - startBitInByte, bitCount);
//                    int bitsToKeep = 8 - startBitInByte - bitsInThisByte;

//                    // Shift value right to align the bits we want to insert with the target location
//                    uint shiftedValue = value >> (bitCount - bitsInThisByte);

//                    // Mask to clear the bits we're about to set
//                    byte mask = (byte)((0xFF >> bitsInThisByte) | (0xFF << (8 - bitsToKeep)));

//                    bytes[i] &= mask;
//                    bytes[i] |= (byte)(shiftedValue << bitsToKeep);

//                    bitCount -= bitsInThisByte;
//                    startBitInByte = 0;
//                }
//            }
//            else
//            {
//                for (int i = endByte; i >= startByte; i--)
//                {
//                    int bitsInThisByte = i == endByte ? (startBit + bitCount - 1) % 8 + 1 : Math.Min(8, bitCount);
//                    int bitsToKeep = bitsInThisByte;

//                    // Shift value left to align the bits we want to insert with the target location
//                    uint shiftedValue = value << (8 - bitsInThisByte);

//                    // Mask to clear the bits we're about to set
//                    byte mask = (byte)((0xFF << bitsInThisByte) | (0xFF >> (8 - bitsToKeep)));

//                    bytes[i] &= mask;
//                    bytes[i] |= (byte)(shiftedValue >> (8 - bitsInThisByte));

//                    bitCount -= bitsInThisByte;
//                }
//            }
//        }
//    }
//}
