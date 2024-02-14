public enum Endianness
{
    LSB,
    MSB
}

class Program
{
    static void Main(string[] args)
    {
        byte[] byteArray = new byte[2];
        byteArray = FillBits((byte[])byteArray.Clone(), 0, 6, 2, Endianness.LSB);
        byteArray = FillBits((byte[])byteArray.Clone(), 10, 6, 1, Endianness.LSB);
        //SetBits((byte[])byteArray, 1, 11, 255, Endianness.MSB);

        Console.WriteLine(BitConverter.ToString(byteArray, 0, 2));
    }
    static byte[] FillBits(byte[] bytes, int startBit, int bitCount, int value, Endianness endianness)
    {
        if(startBit < 0 || startBit >= bytes.Length * 8 || bitCount <  0)
        {
            return null;
        }
        int startByte = startBit / 8;

        int bitIndex = startBit;

        for(int i = startByte; i < bytes.Length; i++)
        {
            int byteValue = 0;
            int bitsToFill = Math.Min(8, bitCount);
            for(int j = 0; j <  8; j++)
            {
                if ((startBit + bitCount) < j)
                    break;
                int bitValue = (value >> j) & 1;
                byteValue |= bitValue << (endianness == Endianness.MSB ? bitIndex : (8* (i + 1))- j);
                bitIndex++;
            }
            bytes[i] = (byte)(byteValue % 0xFF);
            byteValue = 0;
            bitIndex = 0;
        }
        return bytes;
    }
    static void SetBits(byte[] byteArray, int startIndex, int endIndex, int value , Endianness endianness)
    {
        int bitIndex = startIndex;
        for(int i = byteArray.Length -1; i >=0; i--)
        {
            int byteValue = 0;
            int bitsToFill = Math.Min(8, endIndex - bitIndex + 1);
            for(int j = 0; j < bitsToFill; j++)
            {
                int bitValue = (value >> (endianness == Endianness.MSB ? (bitsToFill - j -1) : j)) &1;
                byteValue |= bitValue << (endianness == Endianness.MSB ? (7-j) : j);
                bitIndex++;
            }
            byteArray[i] = (byte)byteValue;
            if(bitIndex > endIndex) break;
        }
    }
    static byte[] SetBits(int startBitIndex, int bitCount, int value, Endianness endianness)
    {
        byte[] result = new byte[(startBitIndex + bitCount + 7) / 8];
        int byteIndex;
        int bitIndex;
        if(endianness == Endianness.LSB)
        {
            byteIndex = startBitIndex / 8;
            bitIndex = startBitIndex % 8;
        }
        else
        {
            byteIndex = (startBitIndex + bitCount - 1) / 8;
            bitIndex = 7 - (startBitIndex % 8);
        }
        for(int i = 0; i < bitCount; i++)
        {
            int bitValue = (value >> (endianness == Endianness.MSB ? bitCount - i -1 : i)) & 1;
            result[byteIndex] |= (byte)(bitValue << bitIndex);
            if(endianness == Endianness.LSB)
            {
                bitIndex++;
                if(bitIndex == 8)
                {
                    byteIndex++;
                    bitIndex = 0;
                }
            }
            else
            {
                bitIndex--;
                if(bitIndex == -1)
                {
                    byteIndex--;
                    bitIndex = 7;
                }
            }
        }
        return result;
    }
}