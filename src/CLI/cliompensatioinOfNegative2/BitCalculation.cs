using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cliompensatioinOfNegative2
{
    public  class BitCalculation
    {
        public  int CalculateComplement(byte[] input, int bitPosition)
        {
            int result = 0;
            //if (BitConverter.IsLittleEndian)
            //{
            //    Array.Reverse(input);
            //}
            for (int i = 0; i < input.Length; i++)
            {
                int mask = (1 << (bitPosition))-1;
                var maskedNum = input[i] & mask;
                
                int singBitMask = 1 <<( bitPosition -1);
                bool isNegative = (maskedNum & singBitMask) != 0;
                if(isNegative)
                {
                    maskedNum = (maskedNum ^ mask) + 1;
                    maskedNum = - maskedNum;
                    return maskedNum;
                }
                result = result | maskedNum;
            }
            return result;
        }
        public double CalculateComplement(double originData, int signIndex)
        {
            int originToInt = Convert.ToInt32(originData);
            int mask = (1 << (signIndex)) - 1;
            bool isNegative = (originToInt & (1 << signIndex - 1)) != 0;
            int convertData = 0;

            if (isNegative)
            {
                convertData = (originToInt ^ mask) + 1;
                convertData *= -1;
            }
            else
            {
                convertData = (int)(originToInt & ((1 << signIndex) - 1));
            }
                
            return convertData;
        }
        public int GetTwoComplement(int number, int bitLength)
        {
            int mask = (1 << bitLength) - 1;
            int trimmedNumber = number % mask;
            int complement = (~trimmedNumber + 1) & mask;
            return complement;
        }
        public int GetOriginalFromTwoComplement(int twosComplement, int bitLength)
        {
            bool isNegative = (twosComplement & (1 << (bitLength -1)))!= 0.0;
            
            if(isNegative)
            {
                int mask = (1 << bitLength) - 1;
                int originalValue = (twosComplement ^ mask) + 1;
                originalValue *= -1;
                return originalValue;
            }
            return twosComplement;

            return 0;
        }
    }
}
