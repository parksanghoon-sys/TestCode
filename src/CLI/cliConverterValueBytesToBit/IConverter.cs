using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cliConverterValueBytesToBit
{
    internal interface IValueToByteConverter
    {
        void FillBits(byte[] bytes, int startBit, int bitCount, uint value, bool fillFromStart);

        uint GetBits(byte[] bytes, int startBit, int bitCount, bool fillFromStart);
    }
}
