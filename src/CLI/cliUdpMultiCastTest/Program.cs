using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

public class BitManipulation
{    
 
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
    public static void Main(string[] args)
    {
        // 모델 데이터를 기반으로 바이트 배열을 생성합니다.
        var model = new DataModel();
        model.PPC_Touch = 1;
        model.SPC_Touch = 1;
        model.PSU_Card_4 = 1;
        model.SW_Version_In_PPC_Minor_Version = 0;
        model.SW_Version_In_PPC_Build_Number = 100;
        model.SW_Version_In_SPC_Device_ID = 54321;
        model.SW_Version_In_SPC_Major_Version = 2;
        model.SW_Version_In_SPC_Minor_Version = 1;
        model.SW_Version_In_SPC_Build_Number = 200;


        // 각 데이터를 바이트 배열에 설정합니다.
        //BitManipulation.SetBitsInByteArray(model.SW_Version_In_PPC, 0, 15, (int)model.SW_Version_In_PPC_Device_ID);
        //BitManipulation.SetBitsInByteArray(model.SW_Version_In_PPC, 16, 23, (int)model.SW_Version_In_PPC_Major_Version);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 0, 1, (int)model.PPC_Touch);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 3, 1, (int)model.SPC_Touch);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 60, 1, (int)model.PSU_Card_4);
        //BitManipulation.SetBitsInByteArray(model.LRU_BIT_RADIO, 32, 47, (int)model.SW_Version_In_PPC_Build_Number);
        //BitManipulation.SetBitsInByteArray(model.LRU_BIT_ANTENA, 48, 63, (int)model.SW_Version_In_SPC_Device_ID);
        //BitManipulation.SetBitsInByteArray(model.LRU_BIT_SPVSR, 64, 71, (int)model.SW_Version_In_SPC_Major_Version);        

        var data = model.SerializeToByteArray();

        // 메시지를 전송합니다.
        Task.Run(() => SendMulticastMessage(data)).Wait();
    }
}

    


// 모델 클래스는 사용자가 제공한 프로퍼티로 정의됩니다.
public class DataModel
{
    public DataModel()
    {
        MessageType = 65502;
        DestinationID = 0x120b0115;
        DestinationID = BitConverter.IsLittleEndian == true ? BinaryPrimitives.ReverseEndianness(DestinationID) : DestinationID;
        MessageType = BitConverter.IsLittleEndian == true ? BinaryPrimitives.ReverseEndianness(MessageType) : MessageType;

        //Array.Reverse(BitConverter.GetBytes(DestinationID).ToArray());
        //Array.Reverse(BitConverter.GetBytes(MessageType).ToArray());
        Timespan = new byte[5];
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
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(ms))
        {
            writer.Write((short)0); 
            writer.Write((short)39);
            writer.Write(0x120b0115);
            writer.Write(DestinationID);
            writer.Write(MessageType);
            writer.Write(MessageType);
            writer.Write(Timespan);

            writer.Write(Total_BIT);
            writer.Write(GCS_Type);            
            writer.Write(SW_Version_In_PPC);
            writer.Write(SW_Version_In_SPC);
            writer.Write(SW_Version_In_MC);
            writer.Write(SW_Version_In_SPV);
            writer.Write(LRU_BIT_MAIN);
            writer.Write(SW_BIT);
            writer.Write(LRU_BIT_RADIO);
            writer.Write(LRU_BIT_ANTENA);
            writer.Write(LRU_BIT_SPVSR);
            writer.Write((byte)0x00);
            writer.Write(SumUp(ms.ToArray()));
            return ms.ToArray();
        }
    }
    public ushort SumUp(byte[] frame)
    {
        ushort num = 0;
        for (int i = 0; i < frame.Length; i++)
        {
            num += frame[i];
        }

        return num;
    }
    public byte[] Timespan { get; set; }    
    public uint SourceID { get; set; }
    public uint DestinationID { get; set; }
    public ushort MessageType { get; set; }
    public byte Total_BIT { get; set; }
    public byte GCS_Type { get; set; }
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
    public double PHONE_BIT { get; set; }
}



