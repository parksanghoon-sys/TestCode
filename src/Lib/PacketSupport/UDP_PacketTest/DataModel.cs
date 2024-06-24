using BytePacketSupport;
using BytePacketSupport.BytePacketSupport.Services.CheckSum;
using System.Reflection;

public class DataModel
{
    public DataModel(bool isAutoInitValue = false)
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
        LRU_BIT_ANTENA = new byte[4];
        LRU_BIT_SPVSR = new byte[1];
        PHONE_BIT = 0x00;
        if (isAutoInitValue)
            InitializeAllPropertiesToOne();


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
            .Compute(CheckSum16Type.NORNAL)
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
    public double PSU_Card_1 { get; set; }
    public double PSU_Card_2 { get; set; }
    public double PSU_Card_3 { get; set; }
    public double PSU_Card_4 { get; set; }
    public double PSU_Card_5 { get; set; }
    public double PSU_Card_6 { get; set; }

    //public double AMU_Card { get; set; }
    //public double MAC_Card_4 { get; set; }
    //public double MAC_Card_3 { get; set; }
    //public double MAC_Card_2 { get; set; }
    //public double MAC_Card_1 { get; set; }
    //public double PCU_Card_4 { get; set; }
    //public double PCU_Card_3 { get; set; }
    //public double PCU_Card_2 { get; set; }
    //public double PCU_Card_1 { get; set; }
    public double ENT_Card_10 { get; set; }
    public double ENT_Card_9 { get; set; }
    public double ENT_Card_8 { get; set; }
    public double ENT_Card_7 { get; set; }
    public double ENT_Card_6 { get; set; }
    public double ENT_Card_5 { get; set; }
    public double ENT_Card_4 { get; set; }
    public double ENT_Card_3 { get; set; }
    public double ENT_Card_2 { get; set; }
    public double ENT_Card_1 { get; set; }

    public double RIO_Card_18 { get; set; }
    public double RIO_Card_17 { get; set; }
    public double RIO_Card_16 { get; set; }
    public double RIO_Card_15 { get; set; }
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
    
    //public double AMU_Card_2 { get; set; }
    //public double RIO_Card_24 { get; set; }
    //public double RIO_Card_23 { get; set; }
    //public double RIO_Card_22 { get; set; }
    //public double RIO_Card_21 { get; set; }
    public byte[] LRU_BIT_SPVSR { get; set; }

    public double ENT_1_Card { get; set; }

    public byte PHONE_BIT { get; set; }

    private void InitializeAllPropertiesToOne()
    {
        Type type = typeof(DataModel);
        PropertyInfo[] properties = type.GetProperties();

        foreach (PropertyInfo prop in properties)
        {
            // 속성이 읽기/쓰기 가능하고, 인덱서가 없는 경우에만 설정
            if (prop.CanWrite && prop.GetIndexParameters().Length == 0)
            {
                // 속성의 타입이 int나 bool인 경우에만 값을 1로 설정 (다른 타입에 대한 처리는 필요에 따라 추가)
                if (prop.PropertyType == typeof(double))
                {
                    prop.SetValue(this, 1);
                }
                else if (prop.PropertyType == typeof(bool))
                {
                    prop.SetValue(this, true);
                }
                // 필요에 따라 다른 타입의 속성도 처리할 수 있음
            }
        }
    }
  
}