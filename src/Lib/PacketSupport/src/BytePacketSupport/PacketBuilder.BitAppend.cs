namespace BytePacketSupport
{
    public partial class PacketBuilder
    {
        public PacketBuilder ParseBitValue<T>(byte[] buffer, int startBit, int endBit, T value, bool isBigEndian = false)
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

            return this;
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


        public static uint GetValue(byte[] buffer, int startBit, int endBit, bool isBigEndian)
        {
            int bitLength = endBit - startBit + 1;
            uint value = 0;

            // Extract bits from the buffer
            for (int i = 0; i < bitLength; i++)
            {
                int byteIndex = (startBit + i) / 8;
                int bitIndex = (startBit + i) % 8;
                int bitValue = (buffer[byteIndex] >> bitIndex) & 1;
                value |= (uint)(bitValue << i);
            }

            // Convert the value to bytes
            byte[] valueBytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian == isBigEndian)
            {
                Array.Reverse(valueBytes);
            }

            // Convert bytes back to uint
            if (isBigEndian)
            {
                value = BitConverter.ToUInt32(valueBytes, 0);
            }
            else
            {
                value = BitConverter.ToUInt32(valueBytes, 0);
            }

            return value;
        }
    }
}
