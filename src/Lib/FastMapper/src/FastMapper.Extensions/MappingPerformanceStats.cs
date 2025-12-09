namespace FastMapper.Extensions.DependencyInjection;

/// <summary>
/// 매핑 성능 통계
/// </summary>
public sealed record MappingPerformanceStats(
    string MapperName,
    long TotalMappings,
    TimeSpan TotalDuration,
    TimeSpan AverageDuration,
    TimeSpan MinDuration,
    TimeSpan MaxDuration,
    DateTime LastMappingTime
);
