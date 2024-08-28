using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public class Crc32
{
    private static readonly uint[] Crc32Table;

    static Crc32()
    {
        Crc32Table = Enumerable.Range(0, 256).Select(i =>
        {
            uint crc = (uint)i;
            for (int j = 8; j > 0; j--)
            {
                if ((crc & 1) == 1)
                {
                    crc = (crc >> 1) ^ 0xEDB88320u;
                }
                else
                {
                    crc >>= 1;
                }
            }
            return crc;
        }).ToArray();
    }

    public static uint ComputeChecksum(byte[] bytes)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in bytes)
        {
            byte tableIndex = (byte)(((crc) & 0xFF) ^ b);
            crc = Crc32Table[tableIndex] ^ (crc >> 8);
        }
        return ~crc;
    }

    public static uint ComputeChecksum(string filePath)
    {
        using (var stream = File.OpenRead(filePath))
        {
            uint crc = 0xFFFFFFFF;
            int byteValue;
            while ((byteValue = stream.ReadByte()) != -1)
            {
                byte tableIndex = (byte)(((crc) & 0xFF) ^ (byte)byteValue);
                crc = Crc32Table[tableIndex] ^ (crc >> 8);
            }
            return ~crc;
        }
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        string filePath = @"D:\Temp\20221125_B_납품_버젼\FlightSolution\AlarmControls\AlarmDataManager.cs"; // 파일 경로를 여기에 입력하세요.
        string fileData = """
                        /**
             *  @file   AlarmDataManager.cs
             *  @date   2015/10/18
             *  @abstract   공영일(yikong@soletop.com)
             *  @brief  알람 데이터들을 관리하는 클래스
             *  @date
             */
            using AlarmControls.WarningAlarm;
            using AvsModule;
            using BViewModelLib;
            using DatabaseManager;
            using FlightModuleEvnetManager;
            #if Bsix
            using BsHostModule;
            #else
            using MHostModule;
            #endif
            using PpcDBSqlControl;
            using Soletop.DataModel;
            using Soletop.LJSON;
            using StDataModel;
            using System;
            using System.Collections.Generic;
            using System.Collections.ObjectModel;
            using System.Configuration;
            using System.Data.SqlClient;
            using System.Linq;
            using System.Text;
            using System.Threading;
            using System.Threading.Tasks;
            using EncryptionModule;
            using StoreControls.Manager;
            using ImageProcessMemoryMapped;
            using System.IO;

            namespace AlarmControls
            {
                /**
                 *  @class  AlarmDataManager
                 *  @brief  알람 데이터들을 관리 및 동작 정의
                 *  @warning 
                 */
                public class AlarmDataManager : PropertyModel
                {
                    private static readonly AlarmDataManager instance = new AlarmDataManager();
                    public static AlarmDataManager Instance
                    {
                        get { return instance; }
                    }

                    // 20220412 조승현 - [자체개선] query 시 충돌방지를 위해 lock 처리
                    private object lockQuery = new object();

                    //private Dictionary<string, AlarmDataModel> alarmDataModelDic = new Dictionary<string, AlarmDataModel>();
                    private List<AlarmDataModel> alarmDataModelList = new List<AlarmDataModel>();
                    private List<AlarmModel> RealTimeAlarmDataSource = new List<AlarmModel>();
                    private List<string> OverLap_Msg = new List<string>();
                    public Dictionary<int, AlarmCondInfo> AlarmCondDic = new Dictionary<int, AlarmCondInfo>();

                    private IpMemoryMapped m_ipmm = new IpMemoryMapped(IpMemoryMapped.MemoryMappedType.LOAD);

                    Timer updateTimer = null;
                    bool IsUpdateEnable = false;
                    int TimerPeriod = 400;

                    bool IsSingleListInit = false;
                    bool IsRaangeListInit = false;
                    bool IsInit = false;
                    bool End_Alarm_check = true;
                    private double prevUnmatchedRunwayId = 0;
                    private readonly string UnmatchedRunwayIdMessage = "UnmatchedRunwayIdAlarm";

                    private double preUnmatchedHandoverType = 0;
                    private readonly string UnmatchedHandoverTypeMessage = "UnmatchedHandoverTypeMessageAlarm";

                    private double prevUnmatchedSafezoneId = 0;
                    private readonly string UnmatchedSafezoneIdMessage = "UnmatchedSafezoneIdAlarm";

                    private double prevUnmatchedReturnId = 0;
                    private readonly string UnmatchedReturnIdMessage = "UnmatchedReturnIdAlarm";

                    private double prevFaultCode = 0;
                    private readonly string FaultCodeMessage = "FaultCodeAlarm";

                    //20190410 박정일 안전지대 항로점 3초 판단
                    bool check_SafezoneID_start = false;
                    bool check_SafezoneID = false;
                    DateTime SafezoneID_time = new DateTime();
                    //20190410 박정일 최단귀환 항로점 3초 판단
                    bool check_ReturnID_start = false;
                    bool check_ReturnID = false;
                    DateTime check_ReturnID_time = new DateTime();    

                    //20180115 박정일 이착륙프로파일 송신여부 판단.
                    bool Check_SendTakeOffProfile = false;
                    /**
                     * @brief  AlarmDataManager 생성자, 선언된 Property 를 초기화 하는 함수 호출
                     * @param
                     * @return
                     * @exception
                     */
                    public AlarmDataManager()
                    {
                        InitAlarm();

                        InitAlarmCondInfo();

                        IsUpdateEnable = true;

                        UpdateStart();

                        RadtCbit = CommonModule.LedStatusControl.LedStateType.Default;
                    }

                    #region 경고 판단 조건 정의
                    string ProfileId = "";
                    string RunwayId = "";

                    public void InitAlarmCondInfo()
                    {
                        AlarmCondDic.Add(1, new AlarmCondInfo() { Name = "활주제원 수신후", IsCheck = false });
                        AlarmCondDic.Add(2, new AlarmCondInfo() { Name = "이륙프로파일 ID 수신후", IsCheck = false });
                        AlarmCondDic.Add(3, new AlarmCondInfo() { Name = "활주이동프로파일 ID 수신후", IsCheck = false });
                        AlarmCondDic.Add(4, new AlarmCondInfo() { Name = "WOW off", IsCheck = false });
                        AlarmCondDic.Add(5, new AlarmCondInfo() { Name = "자동착륙", IsCheck = false });
                        AlarmCondDic.Add(6, new AlarmCondInfo() { Name = "엔진작동", IsCheck = false });
                        AlarmCondDic.Add(7, new AlarmCondInfo() { Name = "착륙모드", IsCheck = false });
                        AlarmCondDic.Add(8, new AlarmCondInfo() { Name = "주발전기 정상", IsCheck = true });
                        AlarmCondDic.Add(9, new AlarmCondInfo() { Name = "상향링크 정상", IsCheck = true });
                        AlarmCondDic.Add(10, new AlarmCondInfo() { Name = "하향링크 정상", IsCheck = true });

                        //AvsControlModule.RealTimeContext.Avs.Status.FLCC_PPC_FLCC_STATUS_1.Bind(FLCC_PPC_FLCC_STATUS_1_PropertyChanged, "CHK_RNWY_CMD", "Flight_Control_Mode", "System_Mode");
                        //20171127 박정일 자동이륙불가/고속활주불가를 FLCC_PPC_FLCC_STATUS_1 내에서 처리하므로 Timespan으로 변경.
                        AvsControlModule.RealTimeContext.Avs.Status.FLCC_PPC_FLCC_STATUS_1.Bind(FLCC_PPC_FLCC_STATUS_1_PropertyChanged, "Timespan");

                        #region Runway Id, Profile Id
                        HostModule.Console.RUNWAYIDINFO.Bind(RUNWAYIDINFO_PropertyChanged, "Timespan");
                        //20170714 박정일 활주이동 불가가 SPC에서만 도시되는 부분 수정
                        //PPC는 자신이 보낸 ProfileID를 받지 못해서 생긴 문제. 따라서 아래 구문 추가
                        HostModule.Console.PROFILEIDINFO.Bind(PROFILEIDINFO_PropertyChanged, "Timespan");
                        GcsModule.PspcModels.PspcModel pspc = GcsModule.GcsControlModule.RealTimeContext.Gcs;
                        if (HostModule.HostType == HostSystemType.HOST_TYPE_PPC)
                        {
                            pspc.Status.SPC_RUNWAYIDINFO_DDS.Bind(RUNWAYID_DDS_PropertyChanged, "Timespan");
                            pspc.Status.SPC_PROFILEID_DDS.Bind(PROFILEID_DDS_PropertyChanged, "Timespan");

                        }
                        else if (HostModule.HostType == HostSystemType.HOST_TYPE_SPC)
                        {
                            pspc.Status.PPC_RUNWAYIDINFO_DDS.Bind(RUNWAYID_DDS_PropertyChanged, "Timespan");
                            pspc.Status.PPC_PROFILEID_DDS.Bind(PROFILEID_DDS_PropertyChanged, "Timespan");

                        }
                        #endregion

                        AvsControlModule.RealTimeContext.Avs.FlccStatus.Bind(WowOnStatus_PropertyChanged, "WowOnStatus");

                        AvsControlModule.RealTimeContext.Avs.Status.FLCC_PPC_IPCMU_STATUS_1.Bind(FLCC_PPC_IPCMU_STATUS_1_PropertyChanged, "ENG_RUNNING");

                        AvsControlModule.RealTimeContext.Avs.Status.IMC_PPC_AVN_STATUS_04.Bind(MainGetStatus_PropertyChanged, "MAIN_GEN_STS");

                        AvsControlModule.RealTimeContext.Avs.Status.FLCC_PPC_PMU_CAM_GUIDED_OFFSET_STATUS.Bind(MainGetStatus_PropertyChanged, "MGEN_STATUS");

                        AvsControlModule.RealTimeContext.Dls.Status.ADTC_PPC_STATUS.Bind(ADTC_PPC_STATUS_PropertyChanged, "DATALINK_LOSS");

                        AvsControlModule.RealTimeContext.Gdt.Status.GDTC_PPC_STATUS.Bind(GDTC_PPC_STATUS_PropertyChanged, "DATALINK_LOSS");

                        HostModule.Console.MISSIONWAYPOINTINFO.Bind(MISSIONWAYPOINTINFO_PropertyChanged, "CurrentWaypointDiscardOilType");

                    }
                    private void PROFILEID_DDS_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                    {
                        GcsModule.PspcModels.PspcModel pspc = GcsModule.GcsControlModule.RealTimeContext.Gcs;

                        if (!pspc.MonitoringSts.CurrentMonitoringStatus) return;

                        System.Text.Encoding utf8 = System.Text.Encoding.UTF8;

                        byte[] data = new byte[255];

                        Array.Clear(data, 0, data.Length);

                        if (HostModule.HostType == HostSystemType.HOST_TYPE_PPC)
                        {
                            data = GetByte(pspc.Status.SPC_PROFILEID_DDS.ProfileIdText);
                        }
                        else if (HostModule.HostType == HostSystemType.HOST_TYPE_SPC)
                        {
                            data = GetByte(pspc.Status.PPC_PROFILEID_DDS.ProfileIdText);
                        }

                        ProfileId = utf8.GetString(data).Trim('\0');
                        //20171127 활주이동 불가 조건이 활주이동프로파일ID 수신후로 바뀌어 Flcc_status 수신시에만 처리하도록 수정
                        //AlarmCondDic[3].IsCheck = IsProfileId(ProfileId);

                    }


                    private void PROFILEIDINFO_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                    {
                        ProfileId = new UnicodeEncoding().GetString(HostModule.Console.PROFILEIDINFO.ProfileIdText).Trim('\0');
                        //20171127 활주이동 불가 조건이 활주이동프로파일ID 수신후로 바뀌어 Flcc_status 수신시에만 처리하도록 수정
                        //AlarmCondDic[3].IsCheck = IsProfileId(ProfileId);

                    }

                    private void RUNWAYID_DDS_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                    {
                        GcsModule.PspcModels.PspcModel pspc = GcsModule.GcsControlModule.RealTimeContext.Gcs;

                        if (!pspc.MonitoringSts.CurrentMonitoringStatus) return;

                        System.Text.Encoding utf8 = System.Text.Encoding.UTF8;

                        byte[] data = new byte[255];

                        Array.Clear(data, 0, data.Length);

                        if (HostModule.HostType == HostSystemType.HOST_TYPE_PPC)
                        {
                            data = GetByte(pspc.Status.SPC_RUNWAYIDINFO_DDS.RunwayIdText1);
                        }
                        else if (HostModule.HostType == HostSystemType.HOST_TYPE_SPC)
                        {
                            data = GetByte(pspc.Status.PPC_RUNWAYIDINFO_DDS.RunwayIdText1);
                        }
                        string temp = utf8.GetString(data).Trim('\0');
                        if (temp == "") return;
                        RunwayId = utf8.GetString(data).Trim('\0');
                        //20171127 자동이륙 불가 조건이 이륙프로파일ID 수신후로 바뀌어 Flcc_status 수신시에만 처리하도록 수정
                        //AlarmCondDic[2].IsCheck = IsRunwayId(RunwayId);
                        //20180115 박정일 활주로id 송신후의 조건 추가
                        Check_SendTakeOffProfile = true;

                    }

                    private void RUNWAYIDINFO_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                    {
                        string temp = new UnicodeEncoding().GetString(HostModule.Console.RUNWAYIDINFO.RunwayIdText).Trim('\0');
                        if (temp == "") return;
                        RunwayId = new UnicodeEncoding().GetString(HostModule.Console.RUNWAYIDINFO.RunwayIdText).Trim('\0');
                        //20171127 자동이륙 불가 조건이 이륙프로파일ID 수신후로 바뀌어 Flcc_status 수신시에만 처리하도록 수정
                        //AlarmCondDic[2].IsCheck = IsRunwayId(RunwayId);
                        //20180115 박정일 활주로id 송신후의 조건 추가
                        Check_SendTakeOffProfile = true;

                    }

                    public byte[] GetByte(char[] data)
                    {
                        byte[] returnData = new byte[255];

                        for (int i = 0; i < data.Length; i++)
                        {
                            returnData[i] = (byte)data[i];
                        }

                        return returnData;
                    }



                    bool IsAddGroundWowOffAlarm = false;

                    private void IsGroundWowOff(bool IsGroundMode)
                    {
                        //if (AlarmCondDic[4].IsCheck & IsGroundMode & !IsAddGroundWowOffAlarm)
                        //{
                        //    AlarmControls.AlarmEventManager.Instance.AddAlarm("지상모드 WOW off", 2, "지상모드 WOW off");
                        //    IsAddGroundWowOffAlarm = true;
                        //}
                        //else
                        //{
                        //    if (IsAddGroundWowOffAlarm) AlarmControls.AlarmEventManager.Instance.RemoveAlarm("지상모드 WOW off", 2, "지상모드 WOW off");
                        //    IsAddGroundWowOffAlarm = false;
                        //}
                        //20171129 박정일 지상모드 wow Off에 대한 조건 수정.
                        if (AlarmCondDic[4].IsCheck & IsGroundMode & !IsAddGroundWowOffAlarm)
                        {
                            AlarmControls.AlarmEventManager.Instance.AddAlarm("지상모드 WOW off", 2, "지상모드 WOW off");
                            IsAddGroundWowOffAlarm = true;
                        }
                        else if (!AlarmCondDic[4].IsCheck || !IsGroundMode)
                        {
                            if (IsAddGroundWowOffAlarm)
                            {
                                AlarmControls.AlarmEventManager.Instance.RemoveAlarm("지상모드 WOW off", 2, "지상모드 WOW off");
                                IsAddGroundWowOffAlarm = false;
                            }
                        }
                    }

                    private bool IsAutoTaxing()
                    {
                        bool returnValue = false;

                        if (AvsControlModule.RealTimeContext.Avs.Status.FLCC_PPC_FLCC_STATUS_1.CHK_RNWY_CMD == 1 & AvsControlModule.RealTimeContext.Avs.Status.FLCC_PPC_FLCC_STATUS_1.System_Mode == 1)
                        {
                            returnValue = true;
                        }
                        else
                        {
                            returnValue = false;
                        }

                        return returnValue;
                    }
                    //자동이륙 프로파일 ID 
                    private bool IsTakeOffProfileID()
                    {
                        bool returnValue = false;
                        ////20171103 SYS-SIT-SPR-631 박정일 wow조건 삭제 지상체모드 조건 넣기.
                        ////20171127 자동이륙 불가 조건이 이륙프로파일ID 수신후로 바뀌어 Flcc_status 수신시에만 처리하도록 수정
                        //double sendTakeoff_ID = AvsControlModule.RealTimeContext.Avs.Control.PPC_FLCC_TO_LD_ID.TO_PROFILE_ID;
                        //double sendlanding_id = AvsControlModule.RealTimeContext.Avs.Control.PPC_FLCC_TO_LD_ID.LD_PROFILE_ID;
                        //double receiveTakeoff_ID = AvsControlModule.RealTimeContext.Avs.Status.FLCC_PPC_FLCC_STATUS_1.TO_PROFILE_ID_CURRENT;
                        //double receivelanding_id = AvsControlModule.RealTimeContext.Avs.Status.FLCC_PPC_FLCC_STATUS_1.LD_PROFILE_ID_CURRENT;
                        //bool runway_check = (sendTakeoff_ID == receiveTakeoff_ID && sendlanding_id == receivelanding_id && receivelanding_id == receiveTakeoff_ID);
                        ////20180817 Check_SendTakeOffProfile 조건 삭제
                        ////20181115 Check_SendTakeOffProfile 조건 추가
                        //if (runway_check & AvsControlModule.RealTimeContext.Avs.Status.FLCC_PPC_FLCC_STATUS_1.System_Mode == 1 & Check_SendTakeOffProfile)
                        //20190410 박정일 이륙파일id가 0이 아니면 송신된것으로 판단
                        if (AvsControlModule.RealTimeContext.Avs.Status.FLCC_PPC_FLCC_STATUS_1.TO_PROFILE_ID_CURRENT != 0 & AvsControlModule.RealTimeContext.Avs.Status.FLCC_PPC_FLCC_STATUS_1.System_Mode == 1)
                        {
                            returnValue = true;
                        }
                        else
                        {
                            returnValue = false;
                        }

                        return returnValue;
                    }
                    //활주이동프로파일 ID
                    //private bool IsRunwayProfileId()
                    //{
                    //    bool returnValue = false;

                    //    if (AvsControlModule.RealTimeContext.Avs.Status.FLCC_PPC_FLCC_STATUS_1.System_Mode == 1)
                    //    {
                    //        returnValue = true;
                    //    }
                    //    else
                    //    {
                    //        returnValue = false;
                    //    }

                    //    return returnValue;
                    //}

                    private void FLCC_PPC_FLCC_STATUS_1_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                    {
                        if (AvsControlModule.RealTimeContext.Avs.Status.FLCC_PPC_FLCC_STATUS_1.Flight_Control_Mode == 83)
                        {
                            AlarmCondDic[5].IsCheck = true;
                        }
                        else
                        {
                            AlarmCondDic[5].IsCheck = false;
                        }

                        if (AvsControlModule.RealTimeContext.Avs.Status.FLCC_PPC_FLCC_STATUS_1.System_Mode == 4 || HostModule.Console.MISSIONWAYPOINTINFO.CurrentWaypointDiscardOilType == 1)
                        {
                            AlarmCondDic[7].IsCheck = true;
                        }
                        else
                        {
                            AlarmCondDic[7].IsCheck = false;
                        }
                        if (!AvsControlModule.RealTimeContext.Avs.FlccStatus.WowOnStatus)
                        {
                            AlarmCondDic[4].IsCheck = true;
                        }
                        else
                        {
                            AlarmCondDic[4].IsCheck = false;
                        }
                        AlarmCondDic[1].IsCheck = IsAutoTaxing();
                        //20171107 SYS-SIT-SPR-631 박정일 wow조건에서 지상체모드조건으로 바뀌어서 체계모드가 바뀔때 확인.
                        AlarmCondDic[2].IsCheck = IsTakeOffProfileID();

                        //AlarmCondDic[3].IsCheck = IsRunwayProfileId();
                        IsGroundWowOff(AvsControlModule.RealTimeContext.Avs.Status.FLCC_PPC_FLCC_STATUS_1.System_Mode == 1);
                    }

                    private void MISSIONWAYPOINTINFO_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                    {
                        if (AvsControlModule.RealTimeContext.Avs.Status.FLCC_PPC_FLCC_STATUS_1.System_Mode == 4 || HostModule.Console.MISSIONWAYPOINTINFO.CurrentWaypointDiscardOilType == 1)
                        {
                            AlarmCondDic[7].IsCheck = true;
                        }
                        else
                        {
                            AlarmCondDic[7].IsCheck = false;
                        }
                    }

                    private void WowOnStatus_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                    {
                        //20171107 Wow조건 삭제로 아래 구문 삭제.
                        //AlarmCondDic[2].IsCheck = IsRunwayId(RunwayId);

                        IsGroundWowOff(AvsControlModule.RealTimeContext.Avs.Status.FLCC_PPC_FLCC_STATUS_1.System_Mode == 1);
                    }

                    private void FLCC_PPC_IPCMU_STATUS_1_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                    {
                        if (AvsControlModule.RealTimeContext.Avs.Status.FLCC_PPC_IPCMU_STATUS_1.ENG_RUNNING == 1)
                        {
                            AlarmCondDic[6].IsCheck = true;
                        }
                        else
                        {
                            AlarmCondDic[6].IsCheck = false;
                        }
                    }

                    private void MainGetStatus_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                    {
                        if (AvsControlModule.RealTimeContext.Avs.Status.IMC_PPC_AVN_STATUS_04.MAIN_GEN_STS == 0 &
                            AvsControlModule.RealTimeContext.Avs.Status.FLCC_PPC_PMU_CAM_GUIDED_OFFSET_STATUS.MGEN_STATUS == 0)
                        {
                            AlarmCondDic[8].IsCheck = true;
                        }
                        else
                        {
                            AlarmCondDic[8].IsCheck = false;
                        }
                    }

                    private void ADTC_PPC_STATUS_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                    {
                        if (AvsControlModule.RealTimeContext.Dls.Status.ADTC_PPC_STATUS.DATALINK_LOSS == 0)
                        {
                            AlarmCondDic[9].IsCheck = true;
                        }
                        else
                        {
                            AlarmCondDic[9].IsCheck = false;
                        }
                    }

                    private void GDTC_PPC_STATUS_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                    {
                        if (AvsControlModule.RealTimeContext.Gdt.Status.GDTC_PPC_STATUS.DATALINK_LOSS == 0)
                        {
                            AlarmCondDic[10].IsCheck = true;
                        }
                        else
                        {
                            AlarmCondDic[10].IsCheck = false;
                        }
                    }

                    #endregion
                    public void SetOverlapMsg(string name, bool overlap)
                    {
                        if (overlap)
                        {
                            if(!OverLap_Msg.Contains(name))
                                OverLap_Msg.Add(name);
                        }
                        else
                            OverLap_Msg.Remove(name);

                        lock (RealTimeAlarmDataSource)
                        {
                            foreach (var x in RealTimeAlarmDataSource.Where(p => p.Mnemonic == name))
                            {
                                x.OverLapMSG = overlap;
                            }
                        }
                    }
                    //정일주석
                    //public bool CheckMSG(string msg)
                    //{
                    //    bool check = false;
                    //    List<AlarmModel> list = GetRealTimeAlarmDic();
                    //    for (int i = 0; i < list.Count; i++)
                    //    {
                    //        if (list[i].Message == msg)
                    //        {
                    //            check = true;
                    //            break;
                    //        }
                    //    }
                    //    return check;
                    //}
                    public List<AlarmModel> GetRealTimeAlarmDic()
                    {
                        lock (RealTimeAlarmDataSource)
                        {
                            return RealTimeAlarmDataSource.ConvertAll(alarmModel => alarmModel != null ? alarmModel.Clone() : new AlarmModel().Clone());
                        }
                    }
                    public List<AlarmModel> GetRealTimeAlarm()
                    {
                        lock (RealTimeAlarmDataSource)
                        {
                            return RealTimeAlarmDataSource;
                        }
                    }

                    private Dictionary<string, object> avsTm = null;
                    private Dictionary<string, object> dlsTm = null;
                    private Dictionary<string, object> gdtTm = null;
                    private Dictionary<string, object> eoirTm = null;
                    private Dictionary<string, object> sarTm = null;
                    private Dictionary<string, object> enctm = null;
                    private Dictionary<string, object> ptstm = null;
            #if Bsix
                    private Dictionary<string, object> smcTm = null;

                    private double WeaponSystemStatus = -1;
            #endif


                    /**
                     * @brief 경고 및 알람 정보 초기화
                     * @param
                     * @return
                     * @exception
                     */
                    public void InitAlarm()
                    {
                        avsTm = AvsControlModule.RealTimeContext.Avs.Telemetry.GetAllProperties();
                        gdtTm = AvsControlModule.RealTimeContext.Gdt.Telemetry.GetAllProperties();
                        dlsTm = AvsControlModule.RealTimeContext.Dls.Telemetry.GetAllProperties();
                        eoirTm = AvsControlModule.RealTimeContext.Eoir.Telemetry.GetAllProperties();
                        sarTm = AvsControlModule.RealTimeContext.Sar.Telemetry.GetAllProperties();
            #if Bsix
                        smcTm = AvsControlModule.RealTimeContext.Smc.Telemetry.GetAllProperties();

                        AvsControlModule.RealTimeContext.Smc.Status.SMC_PPC_SMC_STORE_STATUS.Bind(UpdatedWss, "Weapon_System_Status");

            #endif
                        enctm = EncryptionControlModule.RealTimeContext.EncGdt.Telemetry.GetAllProperties();
                        ptstm = GcsModule.GcsControlModule.RealTimeContext.Gcs.Telemetry.GetAllProperties();
                        SqlConnection GCSGCSB6_Con; 

                        if (!IsSingleListInit)
                        {
                            //20170718 박정일 DB 수정
                            //CSQL.Instance(DB.GCSB6).ExecuteReader("SINGLE_WARNING_CHECK");
                            //20170829 박정일 SetConnection->SetLogConnection로 통일
                            bool check = CSQL.Instance(DB.GCSB6).SetLogConnection("SINGLE_WARNING_CHECK");
                            List<TM_SINGLEWARNING> singleList = null;
                            if (check)
                            {
                                singleList = CSQL.Instance(DB.GCSB6).SchemaData<TM_SINGLEWARNING>();
                                IsSingleListInit = true;
                            }
                            //GCSGCSB6_Con = CSQL.Instance(DB.GCSB6).GetConnection();

                            //if (GCSGCSB6_Con.State == System.Data.ConnectionState.Open)
                            //{
                            //    singleList = CSQL.Instance(DB.GCSB6).SchemaData<TM_SINGLEWARNING>();
                            //    IsSingleListInit = true;
                            //}
                            //else
                            //{
                            //    CSQL.Instance(DB.GCSB6).SetConnection("", "");
                            //}

                            int count = 0; 
                            if (singleList != null) ///< 단일 경고 항목 리스트 불러오기
                            {
                                foreach (TM_SINGLEWARNING a in singleList)
                                {
                                    if (a.WARNING_MSG != "")
                                    {
                                        SingleAlarmDataModel use = new SingleAlarmDataModel()
                                        {
                                            Sequence = a.SW_SEQ,
                                            WarningOn = a.WARNING_ON_YN == 'Y' ? true : false,
                                            RnageAlarmFlag = false,
                                            Mnemonic = a.MNEMONIC,
                                            Repeat = a.REPEAT_YN == 'Y' ? true : false,
                                            SystemCode = a.SUB_SYS_CD,
                                            WarningLevel = a.WARNING_LEVEL.ToString(),
                                            AlarmFilePath = a.SOURCE_FILE_PATH,
                                            WarningMsg = a.WARNING_MSG,
                                            WarningValue = a.WARNING_VALUE,
                                            ActionMsg = a.ACTION_MSG,
                                            WARNING_COND = (int)a.WARNING_COND
                                        };
                                        alarmDataModelList.Add(use);
                                    }
                                    //if (alarmDataModelDic.ContainsKey(a.MNEMONIC) == false && a.WARNING_MSG != "")
                                    //{
                                    //    SingleAlarmDataModel use = new SingleAlarmDataModel()
                                    //    {
                                    //        Sequence = a.SW_SEQ,
                                    //        WarningOn = a.WARNING_ON_YN == 'Y' ? true : false,
                                    //        RnageAlarmFlag = false,
                                    //        Mnemonic = a.MNEMONIC,
                                    //        Repeat = a.REPEAT_YN == 'Y' ? true : false,
                                    //        SystemCode = a.SUB_SYS_CD,
                                    //        WarningLevel = a.WARNING_LEVEL.ToString(),
                                    //        AlarmFilePath = a.SOURCE_FILE_PATH,
                                    //        WarningMsg = a.WARNING_MSG,
                                    //        WarningValue = a.WARNING_VALUE,
                                    //        ActionMsg = a.ACTION_MSG,
                                    //        WARNING_COND = (int)a.WARNING_COND
                                    //    };
                                    //    alarmDataModelDic.Add(a.MNEMONIC, use);
                                    //}
                                }
                            }
                        }

                        if (!IsRaangeListInit)
                        {
                            //20170718 박정일 DB 수정
                            //CSQL.Instance(DB.GCSB6).ExecuteReader("RANGE_WARNING_CHECK");
                            //20170829 박정일 SetConnection->SetLogConnection로 통일
                            bool check = CSQL.Instance(DB.GCSB6).SetLogConnection("RANGE_WARNING_CHECK");
                            List<TM_RANGEWARNING> rangeList = null;
                            if (check)
                            {
                                rangeList = CSQL.Instance(DB.GCSB6).SchemaData<TM_RANGEWARNING>();
                                IsRaangeListInit = true;
                            }
                            //GCSGCSB6_Con = CSQL.Instance(DB.GCSB6).GetConnection();

                            //if (GCSGCSB6_Con.State == System.Data.ConnectionState.Open)
                            //{
                            //    rangeList = CSQL.Instance(DB.GCSB6).SchemaData<TM_RANGEWARNING>();
                            //    IsRaangeListInit = true;
                            //}
                            //else
                            //{
                            //    CSQL.Instance(DB.GCSB6).SetConnection("", "");
                            //}

                            if (rangeList != null) ///< 범위경고 항목 리스트 불러오기
                            {
                                foreach (TM_RANGEWARNING a in rangeList)
                                {
                                    if (a.MIN_RED_MSG != "" && a.MIN_YELLOW_MSG != "" && a.MAX_RED_MSG != "" && a.MAX_YELLOW_MSG != "")
                                    {
                                        RangeAlarmDataModel use = new RangeAlarmDataModel()
                                        {
                                            Sequence = a.RW_SEQ + 10000,
                                            WarningOn = a.WARNING_ON_YN == 'Y' ? true : false,
                                            RnageAlarmFlag = true,
                                            Mnemonic = a.MNEMONIC,
                                            Repeat = a.REPEAT_YN == 'Y' ? true : false,
                                            SystemCode = a.SUB_SYS_CD,
                                            WarningLevel = "",
                                            AlarmFilePath = a.SOURCE_FILE_PATH,
                                            Min = a.MIN,
                                            Max = a.MAX,
                                            MinRed = a.MIN_RED,
                                            MinYellow = a.MIN_YELLOW,
                                            MaxRed = a.MAX_RED,
                                            MaxYellow = a.MAX_YELLOW,
                                            MinRedMsg = a.MIN_RED_MSG,
                                            MinYellowMsg = a.MIN_YELLOW_MSG,
                                            MaxRedMsg = a.MAX_RED_MSG,
                                            MaxYellowMsg = a.MAX_YELLOW_MSG,
                                            ActionMsg = a.ACTION_MSG,
                                            WARNING_COND = (int)a.WARNING_COND
                                        };
                                        alarmDataModelList.Add(use);
                                    }
                                    //if (alarmDataModelDic.ContainsKey(a.MNEMONIC) == false && a.MIN_RED_MSG != "" && a.MIN_YELLOW_MSG != "" && a.MAX_RED_MSG != "" && a.MAX_YELLOW_MSG != "")
                                    //{
                                    //    RangeAlarmDataModel use = new RangeAlarmDataModel()
                                    //    {
                                    //        Sequence = a.RW_SEQ + 10000,
                                    //        WarningOn = a.WARNING_ON_YN == 'Y' ? true : false,
                                    //        RnageAlarmFlag = true,
                                    //        Mnemonic = a.MNEMONIC,
                                    //        Repeat = a.REPEAT_YN == 'Y' ? true : false,
                                    //        SystemCode = a.SUB_SYS_CD,
                                    //        WarningLevel = "",
                                    //        AlarmFilePath = a.SOURCE_FILE_PATH,
                                    //        Min = a.MIN,
                                    //        Max = a.MAX,
                                    //        MinRed = a.MIN_RED,
                                    //        MinYellow = a.MIN_YELLOW,
                                    //        MaxRed = a.MAX_RED,
                                    //        MaxYellow = a.MAX_YELLOW,
                                    //        MinRedMsg = a.MIN_RED_MSG,
                                    //        MinYellowMsg = a.MIN_YELLOW_MSG,
                                    //        MaxRedMsg = a.MAX_RED_MSG,
                                    //        MaxYellowMsg = a.MAX_YELLOW_MSG,
                                    //        ActionMsg = a.ACTION_MSG,
                                    //        WARNING_COND = (int)a.WARNING_COND
                                    //    };
                                    //    alarmDataModelDic.Add(a.MNEMONIC, use);
                                    //}
                                }
                            }
                        }

                        if (IsSingleListInit & IsRaangeListInit)
                        {
                            IsInit = true;
                        }
                    }

                    public bool IsFired = false;
                    private System.Timers.Timer Compare_Timer = new System.Timers.Timer();
                    private byte[] RADT_PPC_CBIT_HEALTH_RESULT_Time;
                    public CommonModule.LedStatusControl.LedStateType RadtCbit { get { return (CommonModule.LedStatusControl.LedStateType)this["RadtCbit"]; } set { this["RadtCbit"] = value; } }
                    private uint RADT_CBIT_Count = 0;

                    private void Compare_Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
                    {
                        if (!RADT_PPC_CBIT_HEALTH_RESULT_Time.Equals(AvsModule.AvsControlModule.RealTimeContext.Smc.Status.RADT_PPC_CBIT_HEALTH_RESULT.Timespan))
                        {
                            RadtCbit = CommonModule.LedStatusControl.LedStateType.Normal;
                            RADT_PPC_CBIT_HEALTH_RESULT_Time = AvsModule.AvsControlModule.RealTimeContext.Smc.Status.RADT_PPC_CBIT_HEALTH_RESULT.Timespan;
                            RADT_CBIT_Count = 0;
                        }
                        else
                        {
                            RADT_CBIT_Count++;

                            if (RADT_CBIT_Count >= 10)
                            {
                                RadtCbit = CommonModule.LedStatusControl.LedStateType.Critical;
                                RADT_CBIT_Count = 0;
                            }
                        }
                    }

                    private void UpdatedWss(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                    {
                        if (WeaponSystemStatus != AvsControlModule.RealTimeContext.Smc.Status.SMC_PPC_SMC_STORE_STATUS.Weapon_System_Status)
                        {
                            if (WeaponSystemStatus == 3 && AvsControlModule.RealTimeContext.Smc.Status.SMC_PPC_SMC_STORE_STATUS.Weapon_System_Status == 6)
                            {
                                IsFired = true;
                                RADT_PPC_CBIT_HEALTH_RESULT_Time = AvsModule.AvsControlModule.RealTimeContext.Smc.Status.RADT_PPC_CBIT_HEALTH_RESULT.Timespan;
                                Compare_Timer.Interval = 100;
                                Compare_Timer.Elapsed += Compare_Timer_Elapsed;
                                Compare_Timer.Start();
                            }
                            else
                            {
                                WeaponSystemStatus = AvsControlModule.RealTimeContext.Smc.Status.SMC_PPC_SMC_STORE_STATUS.Weapon_System_Status;

                                IsFired = false;
                                Compare_Timer.Stop();
                            }
                        }
                    }

                    /**
                    * @brief  업데이트 모드를 시작 하는 함수
                    * @param
                    * @return
                    * @exception
                    */
                    public void UpdateStart()
                    {
                        if (IsUpdateEnable == false) return;
                        updateTimer = new Timer(new TimerCallback(UpdateData), null, TimerPeriod, TimerPeriod);
                    }

                    /**
                    * @brief  업데이트 모드를 종료 하는 함수
                    * @param
                    * @return
                    * @exception
                    */
                    //정일주석
                    //public void UpdateStop()
                    //{
                    //    if (updateTimer != null)
                    //    {
                    //        updateTimer.Dispose();
                    //        updateTimer = null;
                    //    }
                    //}
                    //20171213 박정일 B6-SYS-SIT-SPR-267 00제어권이 있는지 확인,모니터링이거나.
                    private bool SMCControlStatus()
                    {
                        bool Check = false;
                        GcsModule.PspcModels.PspcModel pspc = GcsModule.GcsControlModule.RealTimeContext.Gcs;
                        if (HostModule.HostType == HostSystemType.HOST_TYPE_PPC)
                        {
                            if (pspc.Status.PPC_CONNECTION_INFO.SMCConnection == 1 || pspc.Status.PPC_CONNECTION_INFO.SMCConnection == 2)
                            {
                                Check = true;
                            }
                        }
                        else if (HostModule.HostType == HostSystemType.HOST_TYPE_SPC)
                        {
                            if (pspc.Status.SPC_CONNECTION_INFO.SMCConnection == 1 || pspc.Status.SPC_CONNECTION_INFO.SMCConnection == 2)
                            {
                                Check = true;
                            }
                        }
                        return Check;
                    }
                    /**
                     * @brief 알람 리스트 를 가지고 항목 업데이트 및 알람 감시를 위한 Timer 함수, ViewModelBase에 정의되어 있는 함수를 override해서 사용
                     * @param
                     * @return
                     * @exception
                     */
                    public void CheckTimeAlarm()
                    {
                        if (AvsControlModule.RealTimeContext.Avs.Status.FLCC_PPC_FLCC_STATUS_2.SAFETY_ZONE_ID_CURRENT_VALID_ID != AvsControlModule.RealTimeContext.Avs.Control.PPC_FLCC_SYSTEM_FLCC_MODE.SF_ZONE_WP_ID_ID)
                        {
                            if (!check_SafezoneID_start)
                            {
                                SafezoneID_time = DateTime.Now;
                                check_SafezoneID_start = true;
                            }
                            else
                            {
                                TimeSpan sp = DateTime.Now - SafezoneID_time;
                                if (sp.Seconds >= 3)
                                    check_SafezoneID = true;
                            }
                        }
                        else
                        {
                            check_SafezoneID = false;
                            check_SafezoneID_start = false;
                        }
                        if (AvsControlModule.RealTimeContext.Avs.Status.FLCC_PPC_FLCC_STATUS_2.RTB_ID_CURRENT_VALID_ID != AvsControlModule.RealTimeContext.Avs.Control.PPC_FLCC_SYSTEM_FLCC_MODE.RTB_WP_ID_ID)
                        {
                            if (!check_ReturnID_start)
                            {
                                check_ReturnID_time = DateTime.Now;
                                check_ReturnID_start = true;
                            }
                            else
                            {
                                TimeSpan sp = DateTime.Now - check_ReturnID_time;
                                if (sp.Seconds >= 3)
                                    check_ReturnID = true;
                            }
                        }
                        else
                        {
                            check_ReturnID = false;
                            check_ReturnID_start = false;
                        }
                    }




                    public void UpdateData(object arg)
                    {
                        //UpdateStop();

                        if (!IsInit) InitAlarm();
                        if (!End_Alarm_check)
                            return;
                        CheckTimeAlarm();


                        End_Alarm_check = false;
                        GcsModule.PspcModels.PspcModel gcs = GcsModule.GcsControlModule.RealTimeContext.Gcs;
                        DdsPropertyModel m = null;


                        ///<알람 리스트 를 가지고 항목 업데이트 및 알람 감시
                        for (int i = 0; i < alarmDataModelList.Count; i++)
                        {
                            string x = alarmDataModelList[i].Mnemonic;
                            bool IsUpdate = false;
                            double value = 0.0;
                            DateTime temp = DateTime.Now;

                            if (avsTm.ContainsKey(x))
                            {
                                temp = AvsControlModule.RealTimeContext.Avs.Telemetry[x].Time;
                                value = Convert.ToDouble(AvsControlModule.RealTimeContext.Avs.Telemetry[x].Value);
                                //2017.05.16 지상체 ICD를 이용하여 경고 및 알림 구분 PJI 0-Release 1 - Monitioring 2 - Connection
                                if (HostModule.HostStatus.AvStanagControl != StanagControlType.None) IsUpdate = true;
                                bool avconnect = false;


                                //if (HostModule.HostType == HostSystemType.HOST_TYPE_PPC)
                                //{
                                //    if (a.MonitoringSts.CurrentMissionState)
                                //    {
                                //        if (a.Status.SPC_CONNECTION_INFO.AVConnection != 0) IsUpdate = true;
                                //    }
                                //    else
                                //        if (a.Status.PPC_CONNECTION_INFO.AVConnection != 0) IsUpdate = true;

                                //}
                                //else if (HostModule.HostType == HostSystemType.HOST_TYPE_SPC)
                                //{
                                //    if (a.MonitoringSts.CurrentMonitoringStatus & a.Status.SPC_CONNECTION_INFO.AVConnection != 0) IsUpdate = true;
                                //}
                            }
                            else if (gdtTm.ContainsKey(x))
                            {
                                temp = AvsControlModule.RealTimeContext.Gdt.Telemetry[x].Time;
                                value = Convert.ToDouble(AvsControlModule.RealTimeContext.Gdt.Telemetry[x].Value);
                                if (HostModule.HostStatus.GdtcStanagControl != StanagControlType.None) IsUpdate = true;
                                ////2017.05.16 지상체 ICD를 이용하여 경고 및 알림 구분 PJI 0-Release 1 - Monitioring 2 - Connection
                                //if (HostModule.HostType == HostSystemType.HOST_TYPE_PPC)
                                //{
                                //    if (a.MonitoringSts.CurrentMonitoringStatus & a.Status.PPC_CONNECTION_INFO.GDTCConnection != 0) IsUpdate = true;
                                //}
                                //else if (HostModule.HostType == HostSystemType.HOST_TYPE_SPC)
                                //{
                                //    if (a.MonitoringSts.CurrentMonitoringStatus & a.Status.SPC_CONNECTION_INFO.GDTCConnection != 0) IsUpdate = true;
                                //}

                            }
                            else if (dlsTm.ContainsKey(x))
                            {
                                temp = AvsControlModule.RealTimeContext.Dls.Telemetry[x].Time;
                                value = Convert.ToDouble(AvsControlModule.RealTimeContext.Dls.Telemetry[x].Value);
                                if (HostModule.HostStatus.AdtcStanagControl != StanagControlType.None) IsUpdate = true;
                                ////2017.05.16 지상체 ICD를 이용하여 경고 및 알림 구분 PJI 0-Release 1 - Monitioring 2 - Connection
                                //if (HostModule.HostType == HostSystemType.HOST_TYPE_PPC)
                                //{
                                //    if (a.MonitoringSts.CurrentMonitoringStatus & a.Status.PPC_CONNECTION_INFO.ADTCConnection != 0) IsUpdate = true;
                                //}
                                //else if (HostModule.HostType == HostSystemType.HOST_TYPE_SPC)
                                //{
                                //    if (a.MonitoringSts.CurrentMonitoringStatus & a.Status.SPC_CONNECTION_INFO.ADTCConnection != 0) IsUpdate = true;
                                //}

                            }

                            else if (eoirTm.ContainsKey(x))
                            {
                                temp = AvsControlModule.RealTimeContext.Eoir.Telemetry[x].Time;
                                value = Convert.ToDouble(AvsControlModule.RealTimeContext.Eoir.Telemetry[x].Value);
                                IsUpdate = true;
                            }
                            else if (sarTm.ContainsKey(x))
                            {
                                temp = AvsControlModule.RealTimeContext.Sar.Telemetry[x].Time;
                                value = Convert.ToDouble(AvsControlModule.RealTimeContext.Sar.Telemetry[x].Value);
                                IsUpdate = true;
                            }
            #if Bsix
                            else if (smcTm.ContainsKey(x))
                            {
                                temp = AvsControlModule.RealTimeContext.Smc.Telemetry[x].Time;
                                value = Convert.ToDouble(AvsControlModule.RealTimeContext.Smc.Telemetry[x].Value);
                                //20171213 박정일 B6-SYS-SIT-SPR-267 00제어권이 있는지 확인,모니터링이거나.
                                if (SMCControlStatus()) IsUpdate = true;
                                //if (HostModule.HostStatus.SmcStanagControl != StanagControlType.None || gcs.MonitoringSts.CurrentMonitoringStatus) IsUpdate = true;
                            }
            #endif
                            else if (enctm.ContainsKey(x))
                            {
                                temp = EncryptionControlModule.RealTimeContext.EncGdt.Telemetry[x].Time;
                                value = Convert.ToDouble(EncryptionControlModule.RealTimeContext.EncGdt.Telemetry[x].Value);
                                if (HostModule.HostStatus.GdtcStanagControl != StanagControlType.None) IsUpdate = true;
                                ////2017.05.16 지상체 ICD를 이용하여 경고 및 알림 구분 PJI 0-Release 1 - Monitioring 2 - Connection
                                //if (HostModule.HostType == HostSystemType.HOST_TYPE_PPC)
                                //{
                                //    if (a.MonitoringSts.CurrentMonitoringStatus & a.Status.PPC_CONNECTION_INFO.GDTCConnection != 0) IsUpdate = true;
                                //}
                                //else if (HostModule.HostType == HostSystemType.HOST_TYPE_SPC)
                                //{
                                //    if (a.MonitoringSts.CurrentMonitoringStatus & a.Status.SPC_CONNECTION_INFO.GDTCConnection != 0) IsUpdate = true;
                                //}
                            }
                            else if (ptstm.ContainsKey(x))
                            {
                                temp = gcs.Telemetry[x].Time;
                                value = Convert.ToDouble(gcs.Telemetry[x].Value);
                                IsUpdate = true;
                            }

                            if (temp.Year < 2014) continue;
                            UpdateAlarmProperty(x, alarmDataModelList[i].Sequence, value, IsUpdate);
                        }
                        //DateTime endtime = DateTime.Now;
                        //TimeSpan span = endtime - starttime;
                        if (RealTimeAlarmDataSource.Count < AlarmEventManager.Instance.AlarmRemoveCount)
                        {
                            List<AlarmModel> list = RealTimeAlarmDataSource.Where(p => p.Idx == AlarmEventManager.Instance.AlarmIndex).ToList();
                            int removeCnt = RealTimeAlarmDataSource.Count - AlarmEventManager.Instance.AlarmRemoveCount;
                            if (list.Count != 0 & removeCnt > 0)
                            {
                                for (int i = removeCnt; i > 0; i--)
                                {
                                    DeleteAlarm(list[list.Count - 1], "");
                                }
                            }
                        }
                        double sendTakeoff_ID = AvsControlModule.RealTimeContext.Avs.Control.PPC_FLCC_TO_LD_ID.TO_PROFILE_ID;
                        double sendlanding_id = AvsControlModule.RealTimeContext.Avs.Control.PPC_FLCC_TO_LD_ID.LD_PROFILE_ID;
                        double receiveTakeoff_ID = AvsControlModule.RealTimeContext.Avs.Status.FLCC_PPC_FLCC_STATUS_1.TO_PROFILE_ID_CURRENT;
                        double receivelanding_id = AvsControlModule.RealTimeContext.Avs.Status.FLCC_PPC_FLCC_STATUS_1.LD_PROFILE_ID_CURRENT;
                        //20190103 WCA 2.04버전 적용 
                        bool runway_check = (sendTakeoff_ID != receiveTakeoff_ID) || (sendlanding_id != receivelanding_id);

                        //20170808 박정일 UnmatchedRunwayIdCount를 관리하여 중복도시를 막는다. 
                        if (HostModule.HostStatus.AvStanagControl != StanagControlType.None)
                        {
                            if (runway_check & prevUnmatchedRunwayId == 0)
                            {
                                AlarmControls.AlarmEventManager.Instance.AddAlarm("활주로 ID 불일치", 2, UnmatchedRunwayIdMessage);
                                prevUnmatchedRunwayId = 1;
                            }
                            else if (!runway_check & prevUnmatchedRunwayId == 1)
                            {
                                AlarmControls.AlarmEventManager.Instance.RemoveAlarm("활주로 ID 불일치", 2, UnmatchedRunwayIdMessage);
                                prevUnmatchedRunwayId = 0;
                            }
                        }
                        else if (HostModule.HostStatus.AvStanagControl == StanagControlType.None)
                        {
                            if (prevUnmatchedRunwayId == 1)
                            {
                                AlarmControls.AlarmEventManager.Instance.RemoveAlarm("활주로 ID 불일치", 2, UnmatchedRunwayIdMessage);
                                prevUnmatchedRunwayId = 0;
                            }
                        }
                        bool check_groungmode = (AvsControlModule.RealTimeContext.Avs.Status.FLCC_PPC_FLCC_STATUS_1.System_Mode == 1);
                        bool check_landingmode = (AvsControlModule.RealTimeContext.Avs.Status.FLCC_PPC_FLCC_STATUS_1.System_Mode == 4);
                        if (HostModule.HostStatus.AvStanagControl != StanagControlType.None)
                        {
                            if (!check_groungmode)
                            {
                                if (check_SafezoneID & prevUnmatchedSafezoneId == 0)
                                {
                                    AlarmControls.AlarmEventManager.Instance.AddAlarm("최단 안전지대 ID 불일치", 2, UnmatchedSafezoneIdMessage);
                                    prevUnmatchedSafezoneId = 1;
                                }
                                else if (!check_SafezoneID & prevUnmatchedSafezoneId == 1)
                                {
                                    AlarmControls.AlarmEventManager.Instance.RemoveAlarm("최단 안전지대 ID 불일치", 2, UnmatchedSafezoneIdMessage);
                                    prevUnmatchedSafezoneId = 0;
                                }
                            }
                            else
                            {
                                if (prevUnmatchedSafezoneId == 1)
                                {
                                    AlarmControls.AlarmEventManager.Instance.RemoveAlarm("최단 안전지대 ID 불일치", 2, UnmatchedSafezoneIdMessage);
                                    prevUnmatchedSafezoneId = 0;
                                }
                            }
                        }
                        else if (HostModule.HostStatus.AvStanagControl == StanagControlType.None)
                        {
                            if (prevUnmatchedSafezoneId == 1)
                            {
                                AlarmControls.AlarmEventManager.Instance.RemoveAlarm("최단 안전지대 ID 불일치", 2, UnmatchedSafezoneIdMessage);
                                prevUnmatchedSafezoneId = 0;
                            }
                        }
                        //20181228 최단 귀환 항로점 id 불일치 조건 추가 SAFETY_ZONE_ID_CURRENT

                        bool check_autoReturn = (AvsControlModule.RealTimeContext.Avs.Status.FLCC_PPC_FLCC_STATUS_1.Flight_Control_Mode == 86);
                        //자동귀환이면 미전시 / 체계모드가 지상모드나 착륙모드면 미전시
                        bool total_check = true;
                        if (check_autoReturn || check_groungmode || check_landingmode)
                            total_check = false;
                        //20190110 자동귀환이면 id 불일치 경고를 도시하지 않도록 수정.
                        if (HostModule.HostStatus.AvStanagControl != StanagControlType.None)
                        {
                            if (total_check)
                            {
                                if (check_ReturnID & prevUnmatchedReturnId == 0)
                                {
                                    AlarmControls.AlarmEventManager.Instance.AddAlarm("최단 귀환 항로점 ID 불일치", 2, UnmatchedReturnIdMessage);
                                    prevUnmatchedReturnId = 1;
                                }
                                else if (!check_ReturnID & prevUnmatchedReturnId == 1)
                                {
                                    AlarmControls.AlarmEventManager.Instance.RemoveAlarm("최단 귀환 항로점 ID 불일치", 2, UnmatchedReturnIdMessage);
                                    prevUnmatchedReturnId = 0;
                                }
                            }
                            else
                            {
                                if (prevUnmatchedReturnId == 1)
                                {
                                    AlarmControls.AlarmEventManager.Instance.RemoveAlarm("최단 귀환 항로점 ID 불일치", 2, UnmatchedReturnIdMessage);
                                    prevUnmatchedReturnId = 0;
                                }
                            }

                        }
                        else if (HostModule.HostStatus.AvStanagControl == StanagControlType.None)
                        {
                            if (prevUnmatchedReturnId == 1)
                            {
                                AlarmControls.AlarmEventManager.Instance.RemoveAlarm("최단 귀환 항로점 ID 불일치", 2, UnmatchedReturnIdMessage);
                                prevUnmatchedReturnId = 0;
                            }
                        }

                        //20190117 CDU에서 주요경고를 해제 하지 않았다면 도시. 
                        if (HostModule.HostStatus.AvStanagControl != StanagControlType.None)
                        {
                            if (HostModule.Console.SENDFAULTCODE.FaultCode !=0 & prevFaultCode == 0)
                            {
                                AlarmControls.AlarmEventManager.Instance.AddAlarm("고장코드 해제 필요", 2, FaultCodeMessage);
                                prevFaultCode = 1;
                            }
                            else if (HostModule.Console.SENDFAULTCODE.FaultCode == 0 & prevFaultCode == 1)
                            {
                                AlarmControls.AlarmEventManager.Instance.RemoveAlarm("고장코드 해제 필요", 2, FaultCodeMessage);
                                prevFaultCode = 0;
                            }
                        }
                        else if (HostModule.HostStatus.AvStanagControl == StanagControlType.None)
                        {
                            if (prevFaultCode == 1)
                            {
                                AlarmControls.AlarmEventManager.Instance.RemoveAlarm("고장코드 해제 필요", 2, FaultCodeMessage);
                                prevFaultCode = 0;
                            }
                        }

                        //20171227 박정일 B6-SYS-ETF-SPR-003 활주이동 불가 조건 판단 수정 RTC에서 주기로 주는걸로 판단. 
                        if (HostModule.Console.RECVACKSTATUSRUNWAYMOVEID.IsCheck == 1 & AvsControlModule.RealTimeContext.Avs.Status.FLCC_PPC_FLCC_STATUS_1.System_Mode == 1)
                        {
                            AlarmCondDic[3].IsCheck = true;
                        }
                        else
                        {
                            AlarmCondDic[3].IsCheck = false;
                        }
                        //20180912 박정일 통제권변경 장치 설정 오류 wca1.9 장치 자신의 CUCS ID와 운용설정 화면 내 설정된 통제권획득 장치의 CUCS ID가 동일한 경우
                        if (FlightConfigManager.Instance.GcsType == FlightConfigManager.Instance.HandoverGcsType
                            && (int)HostModule.HostType == FlightConfigManager.Instance.HandoverVsmType - 20)
                        {
                            if (preUnmatchedHandoverType == 0)
                            {
                                AlarmControls.AlarmEventManager.Instance.AddAlarm("통제권변경 장치 설정 오류", 1, UnmatchedHandoverTypeMessage);
                                preUnmatchedHandoverType = 1;
                            }

                        }
                        else
                        {
                            if (preUnmatchedHandoverType == 1)
                            {
                                AlarmControls.AlarmEventManager.Instance.RemoveAlarm("통제권변경 장치 설정 오류", 1, UnmatchedHandoverTypeMessage);
                                preUnmatchedHandoverType = 0;
                            }
                        }

                        // 20220314 조승현 - [sit-995] 경고 및 알림 오류 수정 ( 장비점검창 열지 않아도 주기적으로 Station/Store 데이터 갱신 )
                        if (WeaponModel.Instance().IsStatusUpdate())
                        {
                            WeaponModel.Instance().UpdateData(AvsControlModule.RealTimeContext);

                            // 주기적으로 KP 함수 호출
                            // 20220316 조승현 - [TB-PSPC-1400] KP "Footprint내 장입 표적 없음" 경고 및 알림 판단 조건 수정
                            StoreEnum.SType sType = StoreEnum.SType.NoStore;
                            if (AlarmControls.AlarmExCheckManager.Instance.IsRangeAlarm(ref sType))
                            {
                                if (sType == StoreEnum.SType.KP)
                                {
                                    SaveKPIsAvsInRange();
                                }
                            }
                            else if (m_ipmm != null)
                            {
                                m_ipmm.ReadMemoryMapped();

                                if (m_ipmm.IsKP.Equals(1))
                                {
                                    if (!AlarmControls.AlarmExCheckManager.Instance.TargetSelect) SaveKPIsAvsInRange();
                                    else SaveTargetSelectKP();
                                }
                            }
                        }

                        //20180109 박정일 단일경고 판단후 중복경고 판단하도록 수정.
                        AlarmControls.AlarmExCheckManager.Instance.AvsAlarmSingleCheck();

                        End_Alarm_check = true;
                        //UpdateStart();
                    }


                    // 20220316 조승현 - [TB-PSPC-1400] KP "Footprint내 장입 표적 없음" 경고 및 알림 판단 조건 수정
                    private int Storeidx = -1;

                    private void SaveKPIsAvsInRange()
                    {
                        string currentMissionId = string.Empty;
                        GcsModule.PspcModels.PspcModel pspc = GcsModule.GcsControlModule.RealTimeContext.Gcs;

                        if (BsHostModule.HostModule.HostType == HostSystemType.HOST_TYPE_PPC)
                        {
                            currentMissionId = new string(pspc.Status.PPC_CONNECTION_INFO.MissionID).Replace('\0', ' ').Trim();
                        }
                        else
                        {
                            currentMissionId = new string(pspc.Status.SPC_CONNECTION_INFO.MissionID).Replace('\0', ' ').Trim();
                        }

                        List<Dictionary<string, object>> dic_result = new List<Dictionary<string, object>>();
                        lock (lockQuery)
                        {
                            // 20220905 조승현 - [자체개선] 임무모드/훈련모드 분기.
                            if (HostModule.Console.PSPCSTATUSDDS.Mode == 2)
                            {
                                dic_result = CSQL.Instance(DB.GCSB6).Sendquery("DEBRIEF_NP_TARGET_DIC_SELECT",
                                           new string[] {
                                    currentMissionId,
                                });
                            }
                            else if (HostModule.Console.PSPCSTATUSDDS.Mode == 1)
                            {
                                dic_result = CSQL.Instance(DB.GCSB6).Sendquery("DEBRIEF_NP_TARGET_DIC_SELECT_T",
                                           new string[] {
                                    currentMissionId,
                                });
                            }

                        }

                        if (dic_result != null & dic_result.Count > 0)
                        {
                            string strStationStore = null;

                            int selectedStore = 0;

                            if ((int)AvsControlModule.RealTimeContext.Smc.Status.SMC_PPC_SMC_STORE_STATUS.Store1_Select_Status == 1)
                            {
                                selectedStore = 1;
                            }
                            else if ((int)AvsControlModule.RealTimeContext.Smc.Status.SMC_PPC_SMC_STORE_STATUS.Store2_Select_Status == 1)
                            {
                                selectedStore = 2;
                            }
                            else selectedStore = 0;

                            int selectedStation = 0;
                            if ((int)AvsControlModule.RealTimeContext.Smc.Status.SMC_PPC_SMC_STORE_STATUS.Station1_Select_Status == 1)
                            {
                                selectedStation = 1;
                            }
                            else if ((int)AvsControlModule.RealTimeContext.Smc.Status.SMC_PPC_SMC_STORE_STATUS.Station2_Select_Status == 1)
                            {
                                selectedStation = 2;
                            }
                            else if ((int)AvsControlModule.RealTimeContext.Smc.Status.SMC_PPC_SMC_STORE_STATUS.Station3_Select_Status == 1)
                            {
                                selectedStation = 3;
                            }
                            else if ((int)AvsControlModule.RealTimeContext.Smc.Status.SMC_PPC_SMC_STORE_STATUS.Station4_Select_Status == 1)
                            {
                                selectedStation = 4;
                            }

                            switch (selectedStore)
                            {
                                case 1 :
                                    if (selectedStation == 1)
                                        strStationStore = "1A";
                                    else if (selectedStation == 2)
                                        strStationStore = "2A";
                                    else if (selectedStation == 3)
                                        strStationStore = "3A";
                                    else if (selectedStation == 4)
                                        strStationStore = "4A";
                                    break;
                                    // 투하 후는 Store가 0이 될테니깐 
                                default :
                                    strStationStore = null;
                                    break;
                            }

                            // 투하 전
                            if (!string.IsNullOrEmpty(strStationStore))
                            {
                                if (dic_result[0].ContainsKey(strStationStore))
                                {
                                    Storeidx = Convert.ToInt32(dic_result[0][strStationStore]);

                                    if (Storeidx != -1)
                                    {
                                        List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();

                                        lock (lockQuery)
                                        {
                                            // 20220905 조승현 - [자체개선] 임무모드/훈련모드 분기.
                                            if (HostModule.Console.PSPCSTATUSDDS.Mode == 2)
                                            {
                                                result = CSQL.Instance(DB.GCSB6).Sendquery("DEBRIEF_NP_TARGET_SELECT",
                                               new string[] {
                                            currentMissionId,
                                            "1", //0 참조점, 1 OO 표적, 2 투하지점
                                            Storeidx.ToString(),
                                        });
                                            }
                                            else if (HostModule.Console.PSPCSTATUSDDS.Mode == 1)
                                            {
                                                result = CSQL.Instance(DB.GCSB6).Sendquery("DEBRIEF_NP_TARGET_SELECT_T",
                                               new string[] {
                                            currentMissionId,
                                            "1", //0 참조점, 1 OO 표적, 2 투하지점
                                            Storeidx.ToString(),
                                        });
                                            }

                                        }

                                        if (result != null && result.Count > 0)
                                        //if (result != null && result.Count > 20)
                                        {
                                            if (result[0].Count > 20)
                                            {
                                                TargetInfo tgi = GetKPTargetInfoBeforeRelease(result);
                                                StoreControls.SwModule.ModuleInterface.Instance.GetFootprintDraw(tgi);
                                            }
                                        }
                                    }
                                }
                            }
                            // 투하 후 
                            else
                            {
                                if (IsFired)
                                {
                                    if (Storeidx != -1)
                                    {
                                        //List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();

                                        //lock (lockQuery)
                                        //{
                                        //    result = CSQL.Instance(DB.GCSB6).Sendquery("DEBRIEF_NP_TARGET_SELECT",
                                        //       new string[] {
                                        //    currentMissionId,
                                        //    "1", //0 참조점, 1 OO 표적, 2 투하지점
                                        //    Storeidx.ToString(),
                                        //    });
                                        //}

                                        //if (result != null && result.Count > 0)
                                        //if (result != null && result.Count > 20)
                                        //{
                                            //if (result[0].Count > 20)
                                            //{
                                                //TargetInfo tgi = GetKPTargetInfoAfterRelease(result);
                                                //StoreControls.SwModule.ModuleInterface.Instance.GetFPAfterFootprintDraw(tgi);
                                                StoreControls.SwModule.ModuleInterface.Instance.GetMaxRegionAfterRelease();
                                            //}
                                        //}
                                    }
                                }
                            }
                        }
                    }

                    private void SaveTargetSelectKP()
                    {
                        // KP 표적지정 로직
                        //AlarmControls.AlarmExCheckManager.Instance.TargetSelect_idx
                        string currentMissionId = string.Empty;
                        GcsModule.PspcModels.PspcModel pspc = GcsModule.GcsControlModule.RealTimeContext.Gcs;

                        if (BsHostModule.HostModule.HostType == HostSystemType.HOST_TYPE_PPC)
                        {
                            currentMissionId = new string(pspc.Status.PPC_CONNECTION_INFO.MissionID).Replace('\0', ' ').Trim();
                        }
                        else
                        {
                            currentMissionId = new string(pspc.Status.SPC_CONNECTION_INFO.MissionID).Replace('\0', ' ').Trim();
                        }

                        if (AlarmControls.AlarmExCheckManager.Instance.TargetSelect_idx != -1)
                        {
                            List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();

                            LogLine(string.Format("idx : {0}", AlarmControls.AlarmExCheckManager.Instance.TargetSelect_idx.ToString()));

                            lock (lockQuery)
                            {
                                // 20220905 조승현 - [자체개선] 임무모드/훈련모드 분기.
                                if (HostModule.Console.PSPCSTATUSDDS.Mode == 2)
                                {
                                    result = CSQL.Instance(DB.GCSB6).Sendquery("DEBRIEF_NP_TARGET_SELECT",
                                   new string[] {
                                            currentMissionId,
                                            "1", //0 참조점, 1 OO 표적, 2 투하지점
                                            AlarmControls.AlarmExCheckManager.Instance.TargetSelect_idx.ToString(),
                                            });
                                }
                                else if (HostModule.Console.PSPCSTATUSDDS.Mode == 1)
                                {
                                    result = CSQL.Instance(DB.GCSB6).Sendquery("DEBRIEF_NP_TARGET_SELECT_T",
                                   new string[] {
                                            currentMissionId,
                                            "1", //0 참조점, 1 OO 표적, 2 투하지점
                                            AlarmControls.AlarmExCheckManager.Instance.TargetSelect_idx.ToString(),
                                            });
                                }

                            }

                            if (result != null && result.Count > 0)
                            //if (result != null && result.Count > 20)
                            {
                                if (result[0].Count > 20)
                                {
                                    TargetInfo tgi = GetKPTargetInfoAfterRelease(result);
                                    StoreControls.SwModule.ModuleInterface.Instance.GetFPAfterFootprintDraw(tgi);
                                }
                            }
                        }
                    }

                    public static void LogLine(string message)
                    {
                        string dir = AppDomain.CurrentDomain.BaseDirectory;
                        string path = string.Format("{0}/{1}", dir, "Log.txt");

                        using (FileStream stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                        {
                            StreamWriter writer = new StreamWriter(stream);
                            writer.WriteLine(string.Format("{0}\t{1}", message, DateTime.Now.ToString("yyyy.MM.dd hh:mm:ss.fff")));

                            writer.Flush();
                        }
                    }

                    private TargetInfo GetKPTargetInfoAfterRelease(List<Dictionary<string, object>> result)
                    {
                        TargetInfo tgi = new TargetInfo();

                        //if (RadtCbit == CommonModule.LedStatusControl.LedStateType.Normal)
                        //{
                        //    // ADL 연결
                        //    tgi.Latitude = AvsModule.AvsControlModule.RealTimeContext.Smc.Status.KP_PPC_KP_STATUS.Target_Latitude_STATUS;
                        //    tgi.Longitude = AvsModule.AvsControlModule.RealTimeContext.Smc.Status.KP_PPC_KP_STATUS.Target_Longitude_STATUS;
                        //    tgi.Altitude = AvsModule.AvsControlModule.RealTimeContext.Smc.Status.KP_PPC_KP_STATUS.Target_Altitude_STATUS;
                        //    tgi.ImpactAngle = AvsModule.AvsControlModule.RealTimeContext.Smc.Status.KP_PPC_KP_STATUS.Target_Impact_Angle_STATUS;
                        //    tgi.AzimuthAngle = AvsModule.AvsControlModule.RealTimeContext.Smc.Status.KP_PPC_KP_STATUS.Target_Azimuth_STATUS;
                        //    tgi.TargetID = GetTmTargetId();
                        //}
                        //else if (RadtCbit == CommonModule.LedStatusControl.LedStateType.Critical)
                        //{
                            var sqlGeometry = Microsoft.SqlServer.Types.SqlGeometry.Parse(result[0]["COORD_P"].ToString());
                            if (sqlGeometry != null)
                            {
                                tgi.Latitude = (double)sqlGeometry.STPointN(1).STY;
                                tgi.Longitude = (double)sqlGeometry.STPointN(1).STX;
                                tgi.Altitude = double.Parse(result[0]["ALTITUDE"].ToString());
                            }
                            tgi.ImpactAngle = double.Parse(result[0]["IMPACT_DEG"].ToString());
                            tgi.AzimuthAngle = double.Parse(result[0]["IMPACT_DIR"].ToString());
                            tgi.TargetID = result[0]["STORE_IDX"].ToString();
                        //}

                        tgi.StoreType = StoreEnum.SType.KP;

                        return tgi;
                    }

                    private TargetInfo GetKPTargetInfoBeforeRelease(List<Dictionary<string, object>> result)
                    {
                        TargetInfo tgi = new TargetInfo();

                        var sqlGeometry = Microsoft.SqlServer.Types.SqlGeometry.Parse(result[0]["COORD_P"].ToString());
                        if (sqlGeometry != null)
                        {
                            tgi.Latitude = (double)sqlGeometry.STPointN(1).STY;
                            tgi.Longitude = (double)sqlGeometry.STPointN(1).STX;
                            tgi.Altitude = double.Parse(result[0]["ALTITUDE"].ToString());
                        }
                        tgi.StoreType = StoreEnum.SType.KP;
                        tgi.TargetID = result[0]["STORE_IDX"].ToString();
                        tgi.ImpactAngle = double.Parse(result[0]["IMPACT_DEG"].ToString());
                        tgi.AzimuthAngle = double.Parse(result[0]["IMPACT_DIR"].ToString());

                        byte opMode = byte.Parse(result[0]["OP_MODE"].ToString());
                        tgi.OperationType = (StoreEnum.ControlType)opMode;

                        byte fuzeMode = byte.Parse(result[0]["FUZE_MODE"].ToString());
                        tgi.FuzeType = (StoreEnum.FuzeType)fuzeMode;

                        return tgi;
                    }


                    private string GetTmTargetId()
                    {
                        char[] charArray = new char[16];

                        charArray[0] = AvsModule.AvsControlModule.RealTimeContext.Smc.Status.SMC_PPC_TARGET_CONFIRM.KPTC_TARGET_ID_1_2[0];
                        charArray[1] = AvsModule.AvsControlModule.RealTimeContext.Smc.Status.SMC_PPC_TARGET_CONFIRM.KPTC_TARGET_ID_1_2[1];
                        charArray[2] = AvsModule.AvsControlModule.RealTimeContext.Smc.Status.SMC_PPC_TARGET_CONFIRM.KPTC_TARGET_ID_3_4[0];
                        charArray[3] = AvsModule.AvsControlModule.RealTimeContext.Smc.Status.SMC_PPC_TARGET_CONFIRM.KPTC_TARGET_ID_3_4[1];
                        charArray[4] = AvsModule.AvsControlModule.RealTimeContext.Smc.Status.SMC_PPC_TARGET_CONFIRM.KPTC_TARGET_ID_5_6[0];
                        charArray[5] = AvsModule.AvsControlModule.RealTimeContext.Smc.Status.SMC_PPC_TARGET_CONFIRM.KPTC_TARGET_ID_5_6[1];
                        charArray[6] = AvsModule.AvsControlModule.RealTimeContext.Smc.Status.SMC_PPC_TARGET_CONFIRM.KPTC_TARGET_ID_7_8[0];
                        charArray[7] = AvsModule.AvsControlModule.RealTimeContext.Smc.Status.SMC_PPC_TARGET_CONFIRM.KPTC_TARGET_ID_7_8[1];
                        charArray[8] = AvsModule.AvsControlModule.RealTimeContext.Smc.Status.SMC_PPC_TARGET_CONFIRM.KPTC_TARGET_ID_9_10[0];
                        charArray[9] = AvsModule.AvsControlModule.RealTimeContext.Smc.Status.SMC_PPC_TARGET_CONFIRM.KPTC_TARGET_ID_9_10[1];
                        charArray[10] = AvsModule.AvsControlModule.RealTimeContext.Smc.Status.SMC_PPC_TARGET_CONFIRM.KPTC_TARGET_ID_11_12[0];
                        charArray[11] = AvsModule.AvsControlModule.RealTimeContext.Smc.Status.SMC_PPC_TARGET_CONFIRM.KPTC_TARGET_ID_11_12[1];
                        charArray[12] = AvsModule.AvsControlModule.RealTimeContext.Smc.Status.SMC_PPC_TARGET_CONFIRM.KPTC_TARGET_ID_13_14[0];
                        charArray[13] = AvsModule.AvsControlModule.RealTimeContext.Smc.Status.SMC_PPC_TARGET_CONFIRM.KPTC_TARGET_ID_13_14[1];
                        charArray[14] = AvsModule.AvsControlModule.RealTimeContext.Smc.Status.SMC_PPC_TARGET_CONFIRM.KPTC_TARGET_ID_15_16[0];
                        charArray[15] = AvsModule.AvsControlModule.RealTimeContext.Smc.Status.SMC_PPC_TARGET_CONFIRM.KPTC_TARGET_ID_15_16[0];

                        return new string(charArray);
                    }


                    private int GetWarningLevel(string Level)
                    {
                        if (Level == "W") return 3;
                        else if (Level == "C") return 2;
                        else if (Level == "A") return 1;
                        return 0;
                    }

                    /**
                     * @brief 알림 레벨 판별
                     * @param
                     * @return
                     * @exception
                     */
                    //private int GetWarningLevel(string property, long sequence, double value, ref string msg)
                    //{
                    //    RangeAlarmDataModel model = (RangeAlarmDataModel)GetAlarmData(sequence);

                    //    if (model.MinYellow != null & model.MaxYellow != null & model.MinYellow <= value & model.MaxYellow >= value)
                    //    {
                    //        return 0;
                    //    }
                    //    else if (model.MinRed != null & model.MinRed >= value)  ///<Min 값 경고 항목 검출
                    //    {
                    //        msg = model.MinRedMsg;
                    //        return 3;
                    //    }
                    //    else if (model.MinYellow != null & model.MinYellow > value) ///<Min 값 알림 항목 검출
                    //    {
                    //        msg = model.MinYellowMsg;
                    //        return 2;
                    //    }
                    //    else if (model.MaxYellow != null & model.MaxRed != null & model.MaxYellow < value & model.MaxRed > value) ///<Max 값 알림 항목 검출
                    //    {
                    //        msg = model.MaxYellowMsg;
                    //        return 2;
                    //    }
                    //    else if (model.MaxRed != null & model.MaxRed <= value)///<Max 값 경고 항목 검출
                    //    {
                    //        msg = model.MaxRedMsg;
                    //        return 3;
                    //    }

                    //    msg = "";
                    //    return 0;
                    //}

                    /**
                     * @brief 경고 항목을 식별하여 경고 발생시 화면에 도시 하도록 함
                     * @param
                     * @return
                     * @exception
                     */
                    public AlarmDataModel GetAlarmData(long sequence)
                    {
                        AlarmDataModel data = null;
                        for (int i = 0; i < alarmDataModelList.Count; i++)
                        {
                            if (alarmDataModelList[i].Sequence == sequence)
                            {
                                data = alarmDataModelList[i];
                                break;
                            }
                        }
                        return data;
                    }
                    private void UpdateAlarmProperty(string mnemonic, long sequence, double value, bool IsUpdate)
                    {
                        //AlarmDataModel amodel = alarmDataModelDic[mnemonic] as AlarmDataModel;
                        AlarmDataModel amodel = GetAlarmData(sequence);
                        if (amodel == null) return;
                        //if (amodel.RnageAlarmFlag) ///<범위경고 경고 검출
                        //{
                        //    string msg = "";
                        //    int level = GetWarningLevel(mnemonic, sequence, value, ref msg);
                        //    RangeAlarmDataModel rmodel = amodel as RangeAlarmDataModel;
                        //    //20170907 박정일 &&->&로 바꾸면서 에러발생으로 아래 조건을 세부적으로 구현함.
                        //    //20190121 박정일 기존 로직 원복
                        //    if ((AlarmCondDic.ContainsKey(amodel.WARNING_COND) && !AlarmCondDic[amodel.WARNING_COND].IsCheck) || !IsUpdate)
                        //    //bool alarm_check = (AlarmCondDic.ContainsKey(amodel.WARNING_COND));
                        //    //if(alarm_check)
                        //    //    alarm_check = !AlarmCondDic[amodel.WARNING_COND].IsCheck;
                        //    //if(alarm_check || !IsUpdate)
                        //    {
                        //        //알람삭제
                        //        AlarmModel remove = null;///<범위경고 경고 삭제
                        //        lock (RealTimeAlarmDataSource)
                        //        {
                        //            foreach (var x in RealTimeAlarmDataSource.Where(p => p.Idx == rmodel.Sequence))
                        //            {
                        //                remove = x;
                        //            }
                        //        }
                        //        if (remove != null)
                        //        {
                        //            DeleteAlarm(remove, mnemonic);
                        //        }

                        //        return;
                        //    }

                        //    if (level != 0 & msg != null) ///<범위경고 경고 추가
                        //    {
                        //        ///< 알람 발생
                        //        bool init = false;
                        //        lock (RealTimeAlarmDataSource)
                        //        {
                        //            foreach (var x in RealTimeAlarmDataSource.Where(p => p.Idx == rmodel.Sequence))
                        //            {
                        //                init = true;
                        //                if (value != x.AlarmValue || msg != x.Message)
                        //                {
                        //                    //20190123 박정일 범위경고 소수점 나오는거 수정
                        //                    x.AlarmText = string.Format("{0} : {1}({2})", x.RgstDTime.Split(' ')[1].Split('.')[0], msg, value);
                        //                    x.Message = msg;
                        //                    x.AlarmLevel = level;
                        //                    x.AlarmValue = value;

                        //                    AlarmControls.AlarmEventManager.Instance.AlarmListChange(mnemonic, true);
                        //                }
                        //            }
                        //        }
                        //        if (init == false)
                        //        {
                        //            AlarmModel model = new AlarmModel()
                        //            {
                        //                AlarmText = string.Format("{0} : {1}({2})", DateTime.Now.ToString("HH:mm:ss"), msg, value),
                        //                Message = msg,
                        //                AlarmValue = value,
                        //                Idx = rmodel.Sequence,
                        //                IsChecked = false,
                        //                AlarmLevel = level,
                        //                RgstDTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        //                ActionMsg = rmodel.ActionMsg,

                        //            };
                        //            AddAlarm(model, mnemonic);
                        //        }
                        //    }
                        //    else
                        //    {
                        //        //알람삭제
                        //        AlarmModel remove = null;///<범위경고 경고 삭제
                        //        lock (RealTimeAlarmDataSource)
                        //        {
                        //            foreach (var x in RealTimeAlarmDataSource.Where(p => p.Idx == rmodel.Sequence))
                        //            {
                        //                remove = x;
                        //            }
                        //        }
                        //        if (remove != null)
                        //        {
                        //            DeleteAlarm(remove, mnemonic);
                        //        }
                        //    }
                        //}
                        //else
                        //{
                            SingleAlarmDataModel smodel = amodel as SingleAlarmDataModel;
                            //20170907 박정일 &&->&로 바꾸면서 에러발생으로 아래 조건을 세부적으로 구현함. 
                            //if ((AlarmCondDic.ContainsKey(amodel.WARNING_COND) & !AlarmCondDic[amodel.WARNING_COND].IsCheck) || !IsUpdate)
                            bool alarm_check = (AlarmCondDic.ContainsKey(amodel.WARNING_COND));
                            if (alarm_check)
                                alarm_check = !AlarmCondDic[amodel.WARNING_COND].IsCheck;
                            if (alarm_check || !IsUpdate)
                            {
                                //알람삭제
                                AlarmModel remove = null;
                                lock (RealTimeAlarmDataSource)
                                {
                                    foreach (var x in RealTimeAlarmDataSource.Where(p => p.Idx == smodel.Sequence
                                        || p.Message == smodel.WarningMsg))
                                    {
                                        remove = x;
                                    }
                                }
                                if (remove != null)
                                {
                                    if (remove.Message == smodel.WarningMsg)
                                    {
                                        if (remove.AlarmIndexList.Count != 0)
                                        {
                                            remove.AlarmIndexList.Remove(smodel.Sequence);
                                            if (remove.AlarmIndexList.Count == 0)
                                            {
                                                long idx = remove.Idx;
                                                DeleteAlarm(remove, mnemonic);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        DeleteAlarm(remove, mnemonic);
                                    }
                                }

                                return;
                            }

                            if (smodel.WarningValue == value)///< 경고 추가
                            {
                                //알람 발생
                                bool init = false;
                                lock (RealTimeAlarmDataSource)
                                {
                                    foreach (var x in RealTimeAlarmDataSource.Where(p => p.Idx == smodel.Sequence || p.Message == smodel.WarningMsg))
                                    {
                                        init = true;
                                        if (x.Message == smodel.WarningMsg & !x.AlarmIndexList.Contains(smodel.Sequence))
                                        {
                                            x.AlarmIndexList.Add(smodel.Sequence);
                                        }

                                    }
                                }
                                //for (int i = 0; i < RealTimeAlarmDataSource.Count; i++)
                                //{
                                //    AlarmModel model = RealTimeAlarmDataSource[i];
                                //    if (model.OverLapMSG)
                                //    {
                                //        DeleteAlarm(model, mnemonic);
                                //        init = true;
                                //    }

                                //}
                                //if (OverLap_Msg.Contains(smodel.Mnemonic))
                                //    init = true;

                                if (init == false)
                                {
                                    AlarmModel model = new AlarmModel()
                                    {
                                        AlarmText = string.Format("{0} : {1}", DateTime.Now.ToString("HH:mm:ss"), smodel.WarningMsg),
                                        Message = smodel.WarningMsg,
                                        AlarmValue = value,
                                        Idx = smodel.Sequence,
                                        IsChecked = true,
                                        AlarmLevel = GetWarningLevel(smodel.WarningLevel),
                                        WarningCond = smodel.WARNING_COND,
                                        RgstDTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                                        ActionMsg = smodel.ActionMsg
                                    };
            #if Bsix
                                    //20210105 의미없는 코드로 판단 프로그램 다운으로 인한 삭제
                                    //if (mnemonic.Contains("Station"))
                                    //{
                                    //    string station = mnemonic.Split('@')[1].Substring(0, 8);
                                    //    model.AlarmText = model.AlarmText
                                    //        .Replace("OOOO", AvsControlModule.RealTimeContext.Smc.Telemetry[string.Format("SMC_PPC_SMC_STORE_STATUS_SSS_INVENTORY@{0}_Store_ID", station)].ToString());
                                    //}                        
            #endif
                                    model.AlarmIndexList.Add(model.Idx);
                                    AddAlarm(model, mnemonic);
                                }
                                //알람음 발생 조건
                            }
                            else ///< 경고 삭제
                            {
                                //알람삭제
                                AlarmModel remove = null;
                                lock (RealTimeAlarmDataSource)
                                {
                                    foreach (var x in RealTimeAlarmDataSource.Where(p => p.Idx == smodel.Sequence
                                        || p.Message == smodel.WarningMsg))
                                    {
                                        remove = x;
                                    }
                                }
                                if (remove != null)
                                {
                                    if (remove.Message == smodel.WarningMsg)
                                    {
                                        remove.AlarmIndexList.Remove(smodel.Sequence);
                                        if (remove.AlarmIndexList.Count == 0)
                                        {
                                            long idx = remove.Idx;
                                            DeleteAlarm(remove, mnemonic);
                                        }
                                    }
                                    else
                                    {
                                        DeleteAlarm(remove, mnemonic); 
                                    }
                                }
                            }


                    }

                    /**
                     * @brief 알림 추가 함수
                     * @param
                     * @return
                     * @exception
                     */
                    private void AddAlarm(AlarmModel model, string mnemonic)
                    {
                        lock (RealTimeAlarmDataSource)
                        {
                            model.Mnemonic = mnemonic;
                            RealTimeAlarmDataSource.Insert(0, model);
                        }

                        AlarmControls.AlarmEventManager.Instance.AlarmListChange(mnemonic, true);
                        HostModule.Console.ALARMON.SendDdsToRTC("OnOff", (double)0);
                        //if (HostModule.Console.MONITORINGSETTING.MonitoringCmd != 1)
                        {
                            //20170829 박정일 SetConnection->SetLogConnection로 통일
                            //20170912 PPC/SPC 로그 분리 박정일
                            if (HostModule.HostType == HostSystemType.HOST_TYPE_PPC)
                            {
                                CSQL.Instance(DB.GCSB6).SetLogConnection("EVENT_LOG_SAVE_PPC",
                                   new string[] {
                                    model.Message,
                                    FlightConfigManager.Instance.UserOpMode.ToString(), 
                                    //20180425 경고수준에 따라 메시지 number를 달리하여 저장하도록 수정. 
                                    //FlightModuleEvnetManager.EventDictionaryManager.Instance.EventLogCodeDic["CAUTION"],
                                    FlightModuleEvnetManager.EventDictionaryManager.Instance.GetWaringNumber(model.AlarmLevel),
                                    HostModule.HostType == HostSystemType.HOST_TYPE_PPC ? "0" : "1", //HostModule.HostType.ToString(), 20170608 박정식 TH_SYS_LOG SUB_SYS_CD 해당코드로 입력
                                    model.RgstDTime,
                                    FlightConfigManager.Instance.UserMilitaryNum,HostModule.Console.SELECTAVDID.AvsId.ToString()
                                });
                            }
                            else
                            {
                                CSQL.Instance(DB.GCSB6).SetLogConnection("EVENT_LOG_SAVE_SPC",
                                   new string[] {
                                    model.Message,
                                    FlightConfigManager.Instance.UserOpMode.ToString(), 
                                    //20180425 경고수준에 따라 메시지 number를 달리하여 저장하도록 수정. 
                                    //FlightModuleEvnetManager.EventDictionaryManager.Instance.EventLogCodeDic["CAUTION"],
                                    FlightModuleEvnetManager.EventDictionaryManager.Instance.GetWaringNumber(model.AlarmLevel),
                                    HostModule.HostType == HostSystemType.HOST_TYPE_PPC ? "0" : "1", //HostModule.HostType.ToString(), 20170608 박정식 TH_SYS_LOG SUB_SYS_CD 해당코드로 입력
                                    model.RgstDTime,
                                    FlightConfigManager.Instance.UserMilitaryNum,HostModule.Console.SELECTAVDID.AvsId.ToString()
                                });
                            }
                        }
                    }

                    /**
                     * @brief 알림 삭제 함수
                     * @param
                     * @return
                     * @exception
                     */
                    private void DeleteAlarm(AlarmModel model, string mnemonic)
                    {
                        lock (RealTimeAlarmDataSource)
                        {
                            if (RealTimeAlarmDataSource.Contains(model))
                            { 
                                RealTimeAlarmDataSource.Remove(model);

                                AlarmControls.AlarmEventManager.Instance.AlarmListChange(mnemonic, false);
                                //20170829 모니터링시에도 업데이트 하도록 수정. 박정일
                                //if (HostModule.Console.MONITORINGSETTING.MonitoringCmd != 1)
                                {
                                    //20170829 박정일 SetConnection->SetLogConnection로 통일
                                    //20170912 PPC/SPC 로그 분리 박정일
                                    if (HostModule.HostType == HostSystemType.HOST_TYPE_PPC)
                                    {
                                        CSQL.Instance(DB.GCSB6).SetLogConnection("EVENT_LOG_UPDATE_PPC",
                                           new string[] { DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                                        model.Message,
                                        //20170612 박정일 이전의 값의 입력시간을 읽어오도록 수정.
                                        model.RgstDTime,
                                        HostModule.HostType == HostSystemType.HOST_TYPE_PPC ? "0" : "1"
                                            });
                                    }
                                    else
                                    {
                                            CSQL.Instance(DB.GCSB6).SetLogConnection("EVENT_LOG_UPDATE_SPC",
                                           new string[] { DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                                        model.Message,
                                        //20170612 박정일 이전의 값의 입력시간을 읽어오도록 수정.
                                        model.RgstDTime,
                                        HostModule.HostType == HostSystemType.HOST_TYPE_PPC ? "0" : "1"
                                            });
                                    }
                                }
                            }
                        }

                    }

                    public class AlarmCondInfo : UiModifyViewModel
                    {
                        public string Name { get; set; }
                        public bool IsCheck { get; set; }

                        public AlarmCondInfo()
                        {
                            Name = "";
                            IsCheck = false;
                        }
                    }

                    /**
                   * @brief 인스턴스가 소멸시 수행될 명령
                   * @param
                   * @return
                   * @exception
                   */
                    protected override void Dispose(bool disposing)
                    {
                        base.Dispose(disposing);
                        if (disposing == true)
                        {
                            // 데이터 Dispose 코드 입력
                            if (updateTimer != null)
                            {
                                updateTimer.Dispose();
                            }

                            if (Compare_Timer != null)
                            {
                                Compare_Timer.Dispose();
                            }
                        }
                    }
                }
            }
            
            """;
        var bytes = Encoding.UTF8.GetBytes(fileData);
        //uint crcValue = Crc32.ComputeChecksum(bytes);
        byte[] inputBytes;
        using (FileStream fs = File.OpenRead(filePath))
        {
            inputBytes = new byte[fs.Length];
            await fs.ReadAsync(inputBytes, 0, (int)fs.Length);
        }
        

        uint crcValue = Crc32.ComputeChecksum(inputBytes);
        //string crcHex = "0x" + crcValue.ToString("x8");


        Console.WriteLine($"CRC32: {crcValue:x8}");
    }
}
