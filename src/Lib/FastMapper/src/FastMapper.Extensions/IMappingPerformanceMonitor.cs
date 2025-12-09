namespace FastMapper.Extensions.DependencyInjection;

/// <summary>
/// 성능 모니터링 인터페이스
/// </summary>
public interface IMappingPerformanceMonitor
{
    /// <summary>
    /// 매핑 성능 기록
    /// </summary>
    void RecordMapping(string mapperName, TimeSpan duration, int itemCount = 1);

    /// <summary>
    /// 성능 통계 조회
    /// </summary>
    MappingPerformanceStats GetStatistics(string? mapperName = null);
}
