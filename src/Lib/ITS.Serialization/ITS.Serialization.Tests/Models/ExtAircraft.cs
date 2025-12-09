using ITS.Serialization.Core;
using System.Collections.Generic;

namespace ITS.Serialization.Tests.Models
{
    /// <summary>
    /// 확장 항공기 모델 (ITS 시스템의 ExtAircraft와 유사)
    /// </summary>
    [ITS.Serialization.Core.Serializable]
    public class ExtAircraft
    {
        [SerializableMember(1)]
        public int ID { get; set; }

        [SerializableMember(2)]
        public string Callsign { get; set; }

        [SerializableMember(3)]
        public double Latitude { get; set; }

        [SerializableMember(4)]
        public double Longitude { get; set; }

        [SerializableMember(5)]
        public double Altitude { get; set; }

        [SerializableMember(6)]
        public double Speed { get; set; }

        [SerializableMember(7)]
        public double Heading { get; set; }

        [SerializableMember(8)]
        public List<Waypoint> WaypointList { get; set; }

        public ExtAircraft()
        {
            ID = 0;
            Callsign = string.Empty;
            Latitude = 0.0;
            Longitude = 0.0;
            Altitude = 0.0;
            Speed = 0.0;
            Heading = 0.0;
            WaypointList = new List<Waypoint>();
        }

        public override string ToString()
        {
            return $"ExtAircraft[{ID}] {Callsign} @ ({Latitude:F6}, {Longitude:F6}, {Altitude:F1}m) " +
                   $"Speed={Speed:F1}kt, Heading={Heading:F1}°, Waypoints={WaypointList.Count}";
        }
    }
}
