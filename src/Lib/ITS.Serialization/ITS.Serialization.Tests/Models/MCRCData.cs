using ITS.Serialization.Core;
using System.Collections.Generic;

namespace ITS.Serialization.Tests.Models
{
    /// <summary>
    /// MCRC 데이터 (ITS 시스템의 MCRCData와 유사한 구조)
    /// </summary>
    [ITS.Serialization.Core.Serializable]
    public class MCRCData
    {
        [SerializableMember(1)]
        public int DeviceID { get; set; }

        [SerializableMember(2)]
        public string DeviceName { get; set; }

        [SerializableMember(3)]
        public long Timestamp { get; set; }

        [SerializableMember(4)]
        public List<ExtAircraft> ExtAircraftList { get; set; }

        [SerializableMember(5)]
        public List<Target> TargetList { get; set; }

        public MCRCData()
        {
            DeviceID = 0;
            DeviceName = string.Empty;
            Timestamp = 0;
            ExtAircraftList = new List<ExtAircraft>();
            TargetList = new List<Target>();
        }

        public override string ToString()
        {
            return $"MCRCData[Device={DeviceID}:{DeviceName}] @ {Timestamp} " +
                   $"(Aircraft={ExtAircraftList.Count}, Targets={TargetList.Count})";
        }
    }
}
