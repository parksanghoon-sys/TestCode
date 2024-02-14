using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cliConverterValueBytesToBit
{
    internal class BitHelper4 : IConverter
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
                    int byteIndex = bit / 8;
                    int bitIndex = bit % 8;
                    bool bitValue = (value & (1 << (bit - startBit))) != 0;
                    bytes[byteIndex] = (byte)(bytes[byteIndex] & ~(1 << bitIndex) | (bitValue ? 1 << bitIndex : 0));
                }
            }
            else
            {
                for (int bit = startBit; bit < startBit + bitCount; bit++)
                {
                    int byteIndex = bit / 8;
                    int bitIndex = bit % 8;
                    bool bitValue = (value & (1 << (startBit + bitCount - 1 - bit))) != 0;
                    bytes[byteIndex] = (byte)(bytes[byteIndex] & ~(1 << bitIndex) | (bitValue ? 1 << bitIndex : 0));
                }
            }
        }
    }
}
