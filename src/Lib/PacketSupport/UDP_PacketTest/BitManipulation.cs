public class BitManipulation
{
    public byte[] ParseBitValue<T>(byte[] buffer, int startBit, int endBit, T value, bool isBigEndian = false)
    {
        int bitLength = endBit - startBit + 1;

        byte[] valueBytes = GetBytes(value);
        if (isBigEndian)
        {
            Array.Reverse(valueBytes);
        }

        ulong valueAsInt = 0;
        for (int i = 0; i < valueBytes.Length; i++)
        {
            valueAsInt |= ((ulong)valueBytes[i] << (i * 8));
        }

        ulong mask = (1UL << bitLength) - 1;
        valueAsInt &= mask;

        for (int i = 0; i < bitLength; i++)
        {
            var bitValue = (valueAsInt >> i) & 1UL; // Ensure bitValue is ulong
            var byteIndex = (startBit + i) / 8;
            int bitIndex = (startBit + i) % 8;

            buffer[byteIndex] = (byte)((buffer[byteIndex] & ~(1 << bitIndex)) | ((int)bitValue << bitIndex)); // Cast bitValue to int
        }

        return buffer;
    }

    private byte[] GetBytes<T>(T value)
    {
        if (value is byte b)
            return new byte[] { b };
        if (value is sbyte sb)
            return new byte[] { unchecked((byte)sb) };
        if (value is short s)
            return BitConverter.GetBytes(s);
        if (value is ushort us)
            return BitConverter.GetBytes(us);
        if (value is int i)
            return BitConverter.GetBytes(i);
        if (value is uint ui)
            return BitConverter.GetBytes(ui);
        if (value is long l)
            return BitConverter.GetBytes(l);
        if (value is ulong ul)
            return BitConverter.GetBytes(ul);
        if (value is float f)
            return BitConverter.GetBytes(f);
        if (value is double d)
            return BitConverter.GetBytes(d);

        throw new ArgumentException($"Type {typeof(T)} is not supported.");
    }
    public static void SetBitsInByteArray(byte[] data, int startBit, int bitSize, int value)
    {
        int byteIndex = startBit / 8;
        int bitIndex = startBit % 8;

        // Bitmask to isolate the bits we want to set
        int mask = (1 << bitSize) - 1;

        // Shift the value so that it fits in the designated bit range
        value = (value & mask) << bitIndex;

        // Iterate through the bytes and set the appropriate bits
        while (bitSize > 0)
        {
            // Number of bits we can write to the current byte
            int bitsInCurrentByte = Math.Min(8 - bitIndex, bitSize);

            // Mask to clear the target bits in the current byte
            int byteMask = ((1 << bitsInCurrentByte) - 1) << bitIndex;

            // Clear the target bits and set the new value
            data[byteIndex] = (byte)((data[byteIndex] & ~byteMask) | (value & byteMask));

            // Update the bit counters and indices
            bitSize -= bitsInCurrentByte;
            value >>= bitsInCurrentByte;
            bitIndex = 0;
            byteIndex++;
        }
        //int byteIndex = (data.Length * 8 - 1 - startBit) / 8;
        //int bitIndex = startBit % 8;

        //// Bitmask to isolate the bits we want to set
        //int mask = (1 << bitSize) - 1;

        //// Shift the value so that it fits in the designated bit range
        //value = (value & mask) << bitIndex;

        //// Iterate through the bytes and set the appropriate bits
        //while (bitSize > 0)
        //{
        //    // Number of bits we can write to the current byte
        //    int bitsInCurrentByte = Math.Min(8 - bitIndex, bitSize);

        //    // Mask to clear the target bits in the current byte
        //    int byteMask = ((1 << bitsInCurrentByte) - 1) << bitIndex;

        //    // Clear the target bits and set the new value
        //    data[byteIndex] = (byte)((data[byteIndex] & ~byteMask) | (value & byteMask));

        //    // Update the bit counters and indices
        //    bitSize -= bitsInCurrentByte;
        //    value >>= bitsInCurrentByte;
        //    bitIndex = 0;
        //    byteIndex--;
        //}
    }
    public static void SetBitsInByteArrayLittleEndian(byte[] data, int startBit, int bitSize, int value)
    {
        int byteOffset = startBit / 8;  // 시작하는 바이트 위치
        int bitOffset = startBit % 8;   // 바이트 내에서 시작하는 비트 위치

        // Little Endian에서는 하위 비트부터 채워넣음
        for (int i = 0; i < bitSize; i++)
        {
            int bitIndex = bitOffset + i;
            int byteIndex = byteOffset + (bitIndex / 8);
            int bitPosition = bitIndex % 8;

            // value에서 해당 비트를 가져와서 data에 채워넣음
            bool bitValue = ((value >> i) & 1) == 1;
            data[byteIndex] = (byte)((data[byteIndex] & ~(1 << (7 - bitPosition))) | ((bitValue ? 1 : 0) << (7 - bitPosition)));
        }
    }
}
