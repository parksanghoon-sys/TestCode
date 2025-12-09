namespace FastMapper.Core.Common;

/// <summary>
/// 매핑 옵션
/// </summary>
public sealed record MappingOptions
{
    /// <summary>
    /// null 값 처리 방식
    /// </summary>
    public NullValueHandling NullHandling { get; init; } = NullValueHandling.SetNull;
    
    /// <summary>
    /// 문자열 비교 방식
    /// </summary>
    public StringComparison StringComparison { get; init; } = StringComparison.OrdinalIgnoreCase;
    
    /// <summary>
    /// 최대 중첩 깊이
    /// </summary>
    public int MaxDepth { get; init; } = 10;
    
    /// <summary>
    /// 검증 활성화 여부
    /// </summary>
    public bool EnableValidation { get; init; } = true;
    
    /// <summary>
    /// 성능 모니터링 활성화 여부
    /// </summary>
    public bool EnablePerformanceMonitoring { get; init; } = false;
    
    /// <summary>
    /// 스레드 안전성 보장 여부
    /// </summary>
    public bool ThreadSafe { get; init; } = true;
}
