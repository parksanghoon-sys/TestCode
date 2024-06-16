namespace BytePacketSupport.BytePacketSupport
{
    public partial class PacketBuilder
    {
        public PacketBuilder ParseBitValue<T>(byte[] buffer, int startBit, int endBit, uint value, bool isBigEndian = false)
        {
            int bitLength = endBit - startBit + 1;
            byte[] valueBytes = BitConverter.GetBytes(value);

            uint mask = (uint)((1 << bitLength) - 1);
            value &= mask;

            for(int i = 0; i <bitLength; i++)
            {
                var bitValue = (value >> i) & 1;
                var byteIndex = (startBit + i) / 8;
                int bitIndex = (startBit + i) % 8;

                buffer[byteIndex] = (byte)((buffer[byteIndex] & ~(1 << bitIndex)) | (bitValue << bitIndex));
            }
            return this;
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
