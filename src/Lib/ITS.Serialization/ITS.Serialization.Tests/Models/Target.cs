using ITS.Serialization.Core;

namespace ITS.Serialization.Tests.Models
{
    /// <summary>
    /// 표적 상태
    /// </summary>
    public enum TargetStatus
    {
        Unknown = 0,
        Tracking = 1,
        Lost = 2,
        Identified = 3
    }

    /// <summary>
    /// 표적 모델 (ITS 시스템과 유사)
    /// </summary>
    [ITS.Serialization.Core.Serializable]
    public class Target
    {
        [SerializableMember(1)]
        public int ID { get; set; }

        [SerializableMember(2)]
        public string Name { get; set; }

        [SerializableMember(3)]
        public TargetStatus Status { get; set; }

        [SerializableMember(4)]
        public double Latitude { get; set; }

        [SerializableMember(5)]
        public double Longitude { get; set; }

        [SerializableMember(6)]
        public double Altitude { get; set; }

        public Target()
        {
            ID = 0;
            Name = string.Empty;
            Status = TargetStatus.Unknown;
            Latitude = 0.0;
            Longitude = 0.0;
            Altitude = 0.0;
        }

        public override string ToString()
        {
            return $"Target[{ID}] {Name} - {Status} @ ({Latitude:F6}, {Longitude:F6}, {Altitude:F1}m)";
        }
    }
}
