namespace FastMapper.Core.Common;

/// <summary>
/// 매핑 결과를 나타내는 클래스
/// </summary>
/// <typeparam name="T">결과 타입</typeparam>
public sealed record MappingResult<T>
{
    /// <summary>
    /// 매핑 성공 여부
    /// </summary>
    public bool IsSuccess { get; init; }
    
    /// <summary>
    /// 매핑된 값
    /// </summary>
    public T? Value { get; init; }
    
    /// <summary>
    /// 오류 메시지
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// 예외 정보
    /// </summary>
    public Exception? Exception { get; init; }
    
    /// <summary>
    /// 매핑에 소요된 시간 (밀리초)
    /// </summary>
    public long ElapsedMilliseconds { get; init; }
    
    /// <summary>
    /// 매핑된 속성 개수
    /// </summary>
    public int MappedPropertyCount { get; init; }

    /// <summary>
    /// 성공 결과 생성
    /// </summary>
    public static MappingResult<T> Success(T value, long elapsedMs = 0, int propertyCount = 0) =>
        new()
        {
            IsSuccess = true,
            Value = value,
            ElapsedMilliseconds = elapsedMs,
            MappedPropertyCount = propertyCount
        };

    /// <summary>
    /// 실패 결과 생성
    /// </summary>
    public static MappingResult<T> Failure(string errorMessage, Exception? exception = null) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Exception = exception
        };
}

/// <summary>
/// 매핑 컨텍스트 - 매핑 과정에서 공유되는 정보
/// </summary>
public sealed class MappingContext
{
    /// <summary>
    /// 순환 참조 추적을 위한 객체 캐시
    /// </summary>
    public Dictionary<object, object> ObjectCache { get; } = new();
    
    /// <summary>
    /// 매핑 옵션
    /// </summary>
    public MappingOptions Options { get; set; } = new();
    
    /// <summary>
    /// 사용자 정의 데이터
    /// </summary>
    public Dictionary<string, object> UserData { get; } = new();
    
    /// <summary>
    /// 매핑 통계
    /// </summary>
    public MappingStatistics Statistics { get; } = new();
    
    /// <summary>
    /// 취소 토큰
    /// </summary>
    public CancellationToken CancellationToken { get; set; } = default;

    /// <summary>
    /// 순환 참조 확인
    /// </summary>
    public bool HasCircularReference(object source) => ObjectCache.ContainsKey(source);
    
    /// <summary>
    /// 객체 캐시에 추가
    /// </summary>
    public void AddToCache(object source, object destination) => ObjectCache[source] = destination;
    
    /// <summary>
    /// 캐시에서 객체 조회
    /// </summary>
    public T? GetFromCache<T>(object source) where T : class =>
        ObjectCache.TryGetValue(source, out var cached) ? cached as T : null;
}

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

/// <summary>
/// null 값 처리 방식
/// </summary>
public enum NullValueHandling
{
    /// <summary>
    /// null 값을 그대로 설정
    /// </summary>
    SetNull,
    
    /// <summary>
    /// null 값을 무시 (기존 값 유지)
    /// </summary>
    Ignore,
    
    /// <summary>
    /// 기본값으로 대체
    /// </summary>
    SetDefault
}
