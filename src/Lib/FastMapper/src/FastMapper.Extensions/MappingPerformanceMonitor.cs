using System.Collections.Concurrent;

namespace FastMapper.Extensions.DependencyInjection;

/// <summary>
/// 성능 모니터링 구현
/// </summary>
internal sealed class MappingPerformanceMonitor : IMappingPerformanceMonitor
{
    private readonly ConcurrentDictionary<string, List<MappingRecord>> _records = new();

    public void RecordMapping(string mapperName, TimeSpan duration, int itemCount = 1)
    {
        var record = new MappingRecord(DateTime.UtcNow, duration, itemCount);
        _records.AddOrUpdate(mapperName, 
            new List<MappingRecord> { record },
            (key, existing) => 
            {
                existing.Add(record);
                return existing;
            });
    }

    public MappingPerformanceStats GetStatistics(string? mapperName = null)
    {
        if (string.IsNullOrEmpty(mapperName))
        {
            // 전체 통계
            var allRecords = _records.Values.SelectMany(r => r).ToList();
            return CalculateStats("All", allRecords);
        }

        if (_records.TryGetValue(mapperName, out var records))
        {
            return CalculateStats(mapperName, records);
        }

        return new MappingPerformanceStats(mapperName, 0, TimeSpan.Zero, TimeSpan.Zero, 
            TimeSpan.Zero, TimeSpan.Zero, DateTime.MinValue);
    }

    private static MappingPerformanceStats CalculateStats(string name, List<MappingRecord> records)
    {
        if (!records.Any())
            return new MappingPerformanceStats(name, 0, TimeSpan.Zero, TimeSpan.Zero, 
                TimeSpan.Zero, TimeSpan.Zero, DateTime.MinValue);

        var totalDuration = TimeSpan.FromTicks(records.Sum(r => r.Duration.Ticks));
        var averageDuration = TimeSpan.FromTicks(totalDuration.Ticks / records.Count);
        var minDuration = TimeSpan.FromTicks(records.Min(r => r.Duration.Ticks));
        var maxDuration = TimeSpan.FromTicks(records.Max(r => r.Duration.Ticks));
        var lastMappingTime = records.Max(r => r.Timestamp);

        return new MappingPerformanceStats(name, records.Count, totalDuration, 
            averageDuration, minDuration, maxDuration, lastMappingTime);
    }

    private sealed record MappingRecord(DateTime Timestamp, TimeSpan Duration, int ItemCount);
}
