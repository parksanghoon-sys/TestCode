using ITS.Serialization.Core;

namespace ITS.Serialization.Tests.Models
{
    /// <summary>
    /// 경유점 모델 (ITS 시스템과 유사한 구조)
    /// </summary>
    [ITS.Serialization.Core.Serializable]
    public class Waypoint
    {
        [SerializableMember(1)]
        public int ID { get; set; }

        [SerializableMember(2)]
        public string Name { get; set; }

        [SerializableMember(3)]
        public double Latitude { get; set; }

        [SerializableMember(4)]
        public double Longitude { get; set; }

        [SerializableMember(5)]
        public double Altitude { get; set; }

        public Waypoint()
        {
            ID = 0;
            Name = string.Empty;
            Latitude = 0.0;
            Longitude = 0.0;
            Altitude = 0.0;
        }

        public override string ToString()
        {
            return $"Waypoint[{ID}] {Name} ({Latitude:F6}, {Longitude:F6}, {Altitude:F1}m)";
        }
    }
}
