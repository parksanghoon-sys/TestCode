using System.Buffers.Binary;
using System.Net.Sockets;
using System.Net;
using BytePacketSupport;
using BytePacketSupport.BytePacketSupport.Services.CheckSum;

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
        //int byteIndex = startBit / 8;
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
        //    byteIndex++;
        //}
        int byteIndex = (data.Length * 8 - startBit - bitSize) / 8;
        int bitIndex = (startBit + bitSize) % 8;

        // Bitmask to isolate the bits we want to set
        int mask = (1 << bitSize) - 1;

        // Shift the value so that it fits in the designated bit range
        value = (value & mask) << (8 - bitIndex - bitSize);

        // Iterate through the bytes and set the appropriate bits
        while (bitSize > 0)
        {
            // Number of bits we can write to the current byte
            int bitsInCurrentByte = Math.Min(bitIndex + 1, bitSize);

            // Mask to clear the target bits in the current byte
            int byteMask = ((1 << bitsInCurrentByte) - 1) << (8 - bitIndex - bitsInCurrentByte);

            // Clear the target bits and set the new value
            data[byteIndex] = (byte)((data[byteIndex] & ~byteMask) | (value & byteMask));

            // Update the bit counters and indices
            bitSize -= bitsInCurrentByte;
            value >>= bitsInCurrentByte;
            bitIndex = (bitIndex + bitsInCurrentByte) % 8;
            byteIndex += bitIndex == 0 ? -1 : 0;
        }
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
public class UdpMulticastSender
{
    //private static readonly string MulticastGroupAddress = "10.20.11.31";
    private static readonly string MulticastGroupAddress = "192.168.3.206";
    private static readonly int MulticastGroupPort = 5010;

    public static async Task SendMulticastMessage(byte[] message)
    {
        using (UdpClient udpClient = new UdpClient())
        {
            //udpClient.JoinMulticastGroup(IPAddress.Parse(MulticastGroupAddress));

            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(MulticastGroupAddress), MulticastGroupPort);

            try
            {
                await udpClient.SendAsync(message, message.Length, remoteEndPoint);
                Console.WriteLine("Message sent to multicast group");
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }
        }
    }
}
internal class Program
{
    private static void Main(string[] args)
    {
        var model = new DataModel();
        model.PPC_Touch = 1;
        model.SPC_Touch = 1;
        model.PSU_Card_4 = 1;
        model.AMU_Card_2 = 1;
        model.SPC_Touch_SW = 1;

        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 0, 2, (int)model.PPC_Touch);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 2, 2, (int)model.SPC_Touch);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 46, 2, (int)model.PSU_Card_4);
        BitManipulation.SetBitsInByteArray(model.SW_BIT, 1, 1, (int)model.SPC_Touch_SW);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_ANTENA, 8, 1, (int)model.AMU_Card_2);

        Array.Reverse(model.LRU_BIT_MAIN);
        Array.Reverse(model.SW_BIT);
        Array.Reverse(model.LRU_BIT_ANTENA);

        var data = model.SerializeToByteArray();

        // 메시지를 전송합니다.
        Task.Run(() => UdpMulticastSender.SendMulticastMessage(data)).Wait();
    }
}
public class DataModel
{
    public DataModel()
    {
        MessageType = 65502;
        DestinationID = 0x120b0115;

        //Array.Reverse(BitConverter.GetBytes(DestinationID).ToArray());
        //Array.Reverse(BitConverter.GetBytes(MessageType).ToArray());
        Timespan = new byte[5] { 0x00, 0x00, 0x02, 0x00, 0x00 };
        SW_Version_In_PPC = new byte[4];
        SW_Version_In_SPC = new byte[4];
        SW_Version_In_MC = new byte[4];
        SW_Version_In_SPV = new byte[4];
        LRU_BIT_MAIN = new byte[8];
        SW_BIT = new byte[1];
        LRU_BIT_RADIO = new byte[1];
        LRU_BIT_ANTENA = new byte[2];
        LRU_BIT_SPVSR = new byte[1];
    }
    public byte[] SerializeToByteArray()
    {
        var packet = new PacketBuilder(new PacketBuilderConfiguration() { DefaultEndian = BytePacketSupport.Enums.EEndian.BIG })
            .BeginSection("packet")
            .AppendShort(Sequence)
            .AppendUShort(MessageLength)
            .AppendUInt(SourceID)
            .AppendUInt(DestinationID)
            .AppendUShort(MessageType)
            .AppendUShort(MessageProperties)
            .AppendBytes(Timespan)

            .AppendByte(Total_BIT)
            .AppendByte(GCS_Type)
            .AppendBytes(SW_Version_In_PPC)
            .AppendBytes(SW_Version_In_SPC)
            .AppendBytes(SW_Version_In_MC)
            .AppendBytes(SW_Version_In_SPV)
            .AppendBytes(LRU_BIT_MAIN)
            .AppendBytes(SW_BIT)
            .AppendBytes(LRU_BIT_RADIO)
            .AppendBytes(LRU_BIT_ANTENA)
            .AppendBytes(LRU_BIT_SPVSR)
            .AppendByte(PHONE_BIT)

            .EndSection("packet")
            .Compute(CheckSum16Type.NORNAL, false)
            .Build();
        return packet;
    }

    public short Sequence;

    public ushort MessageLength;

    public uint SourceID;

    public uint DestinationID;

    public ushort MessageType;

    public ushort MessageProperties;

    public byte Total_BIT;

    public byte GCS_Type;
    public ushort Checksum;
    public byte[] Timespan { get; set; }
    public byte[] SW_Version_In_PPC { get; set; }
    public double SW_Version_In_PPC_Device_ID { get; set; }
    public double SW_Version_In_PPC_Major_Version { get; set; }
    public double SW_Version_In_PPC_Minor_Version { get; set; }
    public double SW_Version_In_PPC_Build_Number { get; set; }
    public byte[] SW_Version_In_SPC { get; set; }
    public double SW_Version_In_SPC_Device_ID { get; set; }
    public double SW_Version_In_SPC_Major_Version { get; set; }
    public double SW_Version_In_SPC_Minor_Version { get; set; }
    public double SW_Version_In_SPC_Build_Number { get; set; }
    public byte[] SW_Version_In_MC { get; set; }
    public double SW_Version_In_MC_Device_ID { get; set; }
    public double SW_Version_In_MC_Major_Version { get; set; }
    public double SW_Version_In_MC_Minor_Version { get; set; }
    public double SW_Version_In_MC_Build_Number { get; set; }
    public byte[] SW_Version_In_SPV { get; set; }
    public double SW_Version_In_SPV_Device_ID { get; set; }
    public double SW_Version_In_SPV_Major_Version { get; set; }
    public double SW_Version_In_SPV_Minor_Version { get; set; }
    public double SW_Version_In_SPV_Build_Number { get; set; }
    public byte[] LRU_BIT_MAIN { get; set; }
    public double PSU_Card_2 { get; set; }
    public double PSU_Card_1 { get; set; }
    public double AMU_Card { get; set; }
    public double MAC_Card_4 { get; set; }
    public double MAC_Card_3 { get; set; }
    public double MAC_Card_2 { get; set; }
    public double MAC_Card_1 { get; set; }
    public double PCU_Card_4 { get; set; }
    public double PCU_Card_3 { get; set; }
    public double PCU_Card_2 { get; set; }
    public double PCU_Card_1 { get; set; }
    public double RIO_Card_14 { get; set; }
    public double RIO_Card_13 { get; set; }
    public double RIO_Card_12 { get; set; }
    public double RIO_Card_11 { get; set; }
    public double RIO_Card_10 { get; set; }
    public double RIO_Card_9 { get; set; }
    public double RIO_Card_8 { get; set; }
    public double RIO_Card_7 { get; set; }
    public double RIO_Card_6 { get; set; }
    public double RIO_Card_5 { get; set; }
    public double RIO_Card_4 { get; set; }
    public double RIO_Card_3 { get; set; }
    public double RIO_Card_2 { get; set; }
    public double RIO_Card_1 { get; set; }
    public double SPC_Touch { get; set; }
    public double PPC_Touch { get; set; }
    public byte[] SW_BIT { get; set; }
    public double SPV_Touch_SW { get; set; }
    public double MC_Touch_SW { get; set; }
    public double SPC_Touch_SW { get; set; }
    public double PPC_Touch_SW { get; set; }
    public byte[] LRU_BIT_RADIO { get; set; }
    public double UVHF_RADIO_2 { get; set; }
    public double UVHF_RADIO_1 { get; set; }
    public byte[] LRU_BIT_ANTENA { get; set; }
    public double PSU_Card_3 { get; set; }
    public double AMU_Card_2 { get; set; }
    public double RIO_Card_24 { get; set; }
    public double RIO_Card_23 { get; set; }
    public double RIO_Card_22 { get; set; }
    public double RIO_Card_21 { get; set; }
    public byte[] LRU_BIT_SPVSR { get; set; }
    public double PSU_Card_4 { get; set; }
    public double MAC_Card_5 { get; set; }
    public double PCU_Card_5 { get; set; }
    public byte PHONE_BIT { get; set; }


}