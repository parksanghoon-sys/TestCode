namespace FastMapper.Core.Common;

/// <summary>
/// 매핑 통계
/// </summary>
public sealed class MappingStatistics
{
    /// <summary>
    /// 매핑 시작 시간
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 매핑된 객체 수
    /// </summary>
    public int MappedObjectCount { get; set; }
    
    /// <summary>
    /// 매핑된 속성 수
    /// </summary>
    public int MappedPropertyCount { get; set; }
    
    /// <summary>
    /// 캐시 히트 수
    /// </summary>
    public int CacheHits { get; set; }
    
    /// <summary>
    /// 검증 실패 수
    /// </summary>
    public int ValidationFailures { get; set; }
    
    /// <summary>
    /// 소요 시간 계산
    /// </summary>
    public TimeSpan ElapsedTime => DateTime.UtcNow - StartTime;
}
