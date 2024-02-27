using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cliConverterValueBytesToBit
{
    internal class BitHelper4 : IValueToByteConverter
    {
        public void FillBits(byte[] bytes, int startBit, int bitCount, uint value, bool fillFromStart)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (startBit < 0 || startBit >= bytes.Length * 8) throw new ArgumentOutOfRangeException(nameof(startBit));
            if (bitCount <= 0 || bitCount > 32) throw new ArgumentOutOfRangeException(nameof(bitCount));

            if (fillFromStart)
            {
                for (int bit = startBit; bit < startBit + bitCount; bit++)
                {
                    int byteIndex = bytes.Length - (bit / 8) - 1;
                    int bitIndex = bit % 8;
                    bool bitValue = (value & (1 << (bit - startBit))) != 0;
                    bytes[byteIndex] = (byte)(bytes[byteIndex] | (bitValue ? 1 << bitIndex : 0));
                }
            }
            else
            {
                for (int bit = startBit; bit < startBit + bitCount; bit++)
                {
                    int byteIndex = bytes.Length - (bit / 8) - 1;
                    int bitIndex = bit % 8;
                    bool bitValue = (value & (1 << (startBit + bitCount - 1 - bit))) != 0;
                    bytes[byteIndex] = (byte)(bytes[byteIndex] | (bitValue ? 1 << bitIndex : 0));
                }
            }
        }

        public uint GetBits(byte[] bytes, int startBit, int bitCount, bool fillFromStart)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (startBit < 0 || startBit >= bytes.Length * 8) throw new ArgumentOutOfRangeException(nameof(startBit));
            if (bitCount <= 0 || bitCount > 32) throw new ArgumentOutOfRangeException(nameof(bitCount));

            uint result = 0;
            int endBit = startBit + bitCount;
            for (int i = startBit; i < endBit; i++)
            {
                int byteIndex = bytes.Length - (i / 8) - 1;
                int bitIndex = i % 8;
                if ((bytes[byteIndex] & (1 << bitIndex)) != 0)
                {
                    if (fillFromStart)
                    {
                        result |= (uint)(1 << bitIndex);
                    }
                    else
                    {
                        result |= (uint)(1 << (endBit - i - 1));
                    }
                }
                //else
                //{
                //    if (fillFromStart)
                //    {
                //        result = (result << 1);
                //    }
                //}
            }
            return result;

        }
        private uint ReverseBits(uint value, int bitCount)
        {
            uint result = 0;
            for (int i = 0; i < bitCount; i++)
            {
                result = (result << 1) | (value & 1);
                value >>= 1;
            }
            return result;
        }
    }
}
