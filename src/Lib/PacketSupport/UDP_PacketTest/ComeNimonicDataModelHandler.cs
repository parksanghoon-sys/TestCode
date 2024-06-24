using System.Reflection;

public class ComeNimonicDataModelHandler
{
    private DataModel model = null;
    public DataModel? Model
    {
        get => model; set => model = value;
    }

    public ComeNimonicDataModelHandler()
    {
        model = new DataModel();
    }
    public async Task<int> SendComeUDPNimoic()
    {
        BitManipulation.SetBitsInByteArray(model.SW_Version_In_PPC, 0, 8, (int)model.SW_Version_In_PPC_Build_Number);
        BitManipulation.SetBitsInByteArray(model.SW_Version_In_PPC, 8, 8, (int)model.SW_Version_In_PPC_Minor_Version);
        BitManipulation.SetBitsInByteArray(model.SW_Version_In_PPC, 16, 8, (int)model.SW_Version_In_PPC_Major_Version);
        BitManipulation.SetBitsInByteArray(model.SW_Version_In_PPC, 24, 8, (int)model.SW_Version_In_PPC_Device_ID);

        BitManipulation.SetBitsInByteArray(model.SW_Version_In_SPC, 0, 8, (int)model.SW_Version_In_SPC_Build_Number);
        BitManipulation.SetBitsInByteArray(model.SW_Version_In_SPC, 8, 8, (int)model.SW_Version_In_SPC_Minor_Version);
        BitManipulation.SetBitsInByteArray(model.SW_Version_In_SPC, 16, 8, (int)model.SW_Version_In_SPC_Major_Version);
        BitManipulation.SetBitsInByteArray(model.SW_Version_In_SPC, 24, 8, (int)model.SW_Version_In_SPC_Device_ID);

        BitManipulation.SetBitsInByteArray(model.SW_Version_In_MC, 0, 8, (int)model.SW_Version_In_MC_Build_Number);
        BitManipulation.SetBitsInByteArray(model.SW_Version_In_MC, 8, 8, (int)model.SW_Version_In_MC_Minor_Version);
        BitManipulation.SetBitsInByteArray(model.SW_Version_In_MC, 16, 8, (int)model.SW_Version_In_MC_Major_Version);
        BitManipulation.SetBitsInByteArray(model.SW_Version_In_MC, 24, 8, (int)model.SW_Version_In_MC_Device_ID);

        BitManipulation.SetBitsInByteArray(model.SW_Version_In_SPV, 0, 8, (int)model.SW_Version_In_SPV_Build_Number);
        BitManipulation.SetBitsInByteArray(model.SW_Version_In_SPV, 8, 8, (int)model.SW_Version_In_SPV_Minor_Version);
        BitManipulation.SetBitsInByteArray(model.SW_Version_In_SPV, 16, 8, (int)model.SW_Version_In_SPV_Major_Version);
        BitManipulation.SetBitsInByteArray(model.SW_Version_In_SPV, 24, 8, (int)model.SW_Version_In_SPV_Device_ID);

        // Assuming model is an instance of a class containing LRU_BIT_MAIN and other fields
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 0, 2, (int)model.PPC_Touch);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 2, 2, (int)model.SPC_Touch);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 8, 2, (int)model.RIO_Card_1);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 10, 2, (int)model.RIO_Card_2);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 12, 2, (int)model.RIO_Card_3);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 14, 2, (int)model.RIO_Card_4);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 16, 2, (int)model.RIO_Card_5);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 18, 2, (int)model.RIO_Card_6);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 20, 2, (int)model.RIO_Card_7);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 22, 2, (int)model.RIO_Card_8);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 24, 2, (int)model.RIO_Card_9);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 26, 2, (int)model.RIO_Card_10);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 30, 2, (int)model.ENT_Card_1);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 32, 2, (int)model.ENT_Card_2);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 34, 2, (int)model.ENT_Card_3);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 36, 2, (int)model.ENT_Card_4);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 38, 2, (int)model.ENT_Card_5);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 40, 2, (int)model.ENT_Card_6);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 42, 2, (int)model.ENT_Card_7);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 44, 2, (int)model.ENT_Card_8);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 46, 2, (int)model.ENT_Card_9);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 48, 2, (int)model.ENT_Card_10);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 54, 2, (int)model.PSU_Card_1);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 56, 2, (int)model.PSU_Card_2);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 58, 2, (int)model.PSU_Card_3);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_MAIN, 60, 2, (int)model.PSU_Card_4);


        BitManipulation.SetBitsInByteArray(model.SW_BIT, 0, 1, (int)model.PPC_Touch_SW);
        BitManipulation.SetBitsInByteArray(model.SW_BIT, 1, 1, (int)model.SPC_Touch_SW);
        BitManipulation.SetBitsInByteArray(model.SW_BIT, 2, 1, (int)model.MC_Touch_SW);
        BitManipulation.SetBitsInByteArray(model.SW_BIT, 3, 1, (int)model.SPV_Touch_SW);

        BitManipulation.SetBitsInByteArray(model.LRU_BIT_RADIO, 0, 2, (int)model.UVHF_RADIO_1);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_RADIO, 2, 2, (int)model.UVHF_RADIO_2);

        BitManipulation.SetBitsInByteArray(model.LRU_BIT_ANTENA, 0, 2, (int)model.RIO_Card_11);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_ANTENA, 2, 2, (int)model.RIO_Card_12);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_ANTENA, 4, 2, (int)model.RIO_Card_13);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_ANTENA, 6, 2, (int)model.RIO_Card_14);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_ANTENA, 8, 2, (int)model.RIO_Card_15);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_ANTENA, 10, 2, (int)model.RIO_Card_16);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_ANTENA, 12, 2, (int)model.RIO_Card_17);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_ANTENA, 14, 2, (int)model.RIO_Card_18);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_ANTENA, 18, 2, (int)model.PSU_Card_5);
        BitManipulation.SetBitsInByteArray(model.LRU_BIT_ANTENA, 20, 2, (int)model.PSU_Card_6);

        BitManipulation.SetBitsInByteArray(model.LRU_BIT_SPVSR, 0, 2, (int)model.ENT_1_Card);

        var data = model.SerializeToByteArray();

        // 메시지를 전송합니다.
        
        var issuccess = await UdpMulticastSender.SendMulticastMessage(data);

        return issuccess;

    }
    public IEnumerable<string> GetModelsNameString()
    {
        List<string> list = new List<string>();
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
                    list.Add(prop.Name);

                }
                else if (prop.PropertyType == typeof(bool))
                {
                    prop.SetValue(this, true);
                }
                // 필요에 따라 다른 타입의 속성도 처리할 수 있음
            }
        }
        return list;
    }
}
