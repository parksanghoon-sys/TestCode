# 📖 API 레퍼런스

FastMapper의 모든 API에 대한 상세한 레퍼런스입니다.

## 📋 목차

- [핵심 인터페이스](#핵심-인터페이스)
- [어트리뷰트](#어트리뷰트)
- [의존성 주입](#의존성-주입)
- [설정 옵션](#설정-옵션)
- [성능 모니터링](#성능-모니터링)
- [예외 처리](#예외-처리)

## 🔧 핵심 인터페이스

### IMapper<TSource, TDestination>

단방향 매핑을 위한 기본 인터페이스입니다.

```csharp
namespace FastMapper.Core.Abstractions;

public interface IMapper<in TSource, out TDestination> 
    where TSource : class 
    where TDestination : class
{
    /// <summary>
    /// 단일 객체를 매핑합니다.
    /// </summary>
    /// <param name="source">매핑할 소스 객체</param>
    /// <returns>매핑된 대상 객체</returns>
    /// <exception cref="ArgumentNullException">source가 null인 경우</exception>
    TDestination Map(TSource source);
    
    /// <summary>
    /// 컬렉션을 고성능으로 매핑합니다.
    /// </summary>
    /// <param name="sources">매핑할 소스 컬렉션</param>
    /// <returns>매핑된 대상 컬렉션</returns>
    /// <exception cref="ArgumentNullException">sources가 null인 경우</exception>
    IEnumerable<TDestination> MapCollection(IEnumerable<TSource> sources);
    
    /// <summary>
    /// 컬렉션을 비동기로 매핑합니다.
    /// </summary>
    /// <param name="sources">매핑할 소스 컬렉션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>매핑된 대상 컬렉션</returns>
    /// <exception cref="ArgumentNullException">sources가 null인 경우</exception>
    /// <exception cref="OperationCanceledException">작업이 취소된 경우</exception>
    Task<IReadOnlyList<TDestination>> MapCollectionAsync(
        IEnumerable<TSource> sources, 
        CancellationToken cancellationToken = default);
}
```

**사용 예시:**
```csharp
public class UserService
{
    private readonly IMapper<User, UserDto> _mapper;
    
    public UserService(IMapper<User, UserDto> mapper)
    {
        _mapper = mapper;
    }
    
    public UserDto GetUser(User user) => _mapper.Map(user);
    
    public List<UserDto> GetUsers(List<User> users) => 
        _mapper.MapCollection(users).ToList();
}
```

### IBidirectionalMapper<TFirst, TSecond>

양방향 매핑을 위한 인터페이스입니다.

```csharp
namespace FastMapper.Core.Abstractions;

public interface IBidirectionalMapper<TFirst, TSecond> 
    where TFirst : class 
    where TSecond : class
{
    /// <summary>
    /// 첫 번째 타입에서 두 번째 타입으로 매핑합니다.
    /// </summary>
    /// <param name="source">소스 객체</param>
    /// <returns>매핑된 객체</returns>
    TSecond MapTo(TFirst source);
    
    /// <summary>
    /// 두 번째 타입에서 첫 번째 타입으로 매핑합니다.
    /// </summary>
    /// <param name="source">소스 객체</param>
    /// <returns>매핑된 객체</returns>
    TFirst MapFrom(TSecond source);
}
```

**사용 예시:**
```csharp
[MapTo(typeof(UserDto), IsBidirectional = true)]
public class User { /* ... */ }

public class UserService
{
    private readonly IBidirectionalMapper<User, UserDto> _mapper;
    
    public async Task<UserDto> CreateUserAsync(UserDto dto)
    {
        var user = _mapper.MapFrom(dto); // DTO → Entity
        user = await _repository.SaveAsync(user);
        return _mapper.MapTo(user); // Entity → DTO
    }
}
```

### IDynamicMapper

런타임 타입 결정을 위한 동적 매핑 인터페이스입니다.

```csharp
namespace FastMapper.Core.Abstractions;

public interface IDynamicMapper
{
    /// <summary>
    /// 동적 타입 매핑을 수행합니다.
    /// </summary>
    /// <typeparam name="T">대상 타입</typeparam>
    /// <param name="source">소스 객체</param>
    /// <returns>매핑된 객체 또는 null</returns>
    T? Map<T>(object source) where T : class;
    
    /// <summary>
    /// 매핑 지원 여부를 확인합니다.
    /// </summary>
    /// <param name="sourceType">소스 타입</param>
    /// <param name="destinationType">대상 타입</param>
    /// <returns>지원 여부</returns>
    bool CanMap(Type sourceType, Type destinationType);
}
```

## 🏷️ 어트리뷰트

### MapToAttribute

클래스 레벨에서 매핑 대상을 지정하는 어트리뷰트입니다.

```csharp
namespace FastMapper.Core.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class MapToAttribute : Attribute
{
    /// <summary>
    /// 매핑 대상 타입을 지정하여 어트리뷰트를 초기화합니다.
    /// </summary>
    /// <param name="targetType">매핑 대상 타입</param>
    /// <exception cref="ArgumentNullException">targetType이 null인 경우</exception>
    public MapToAttribute(Type targetType);
    
    /// <summary>매핑 대상 타입</summary>
    public Type TargetType { get; }
    
    /// <summary>매핑 프로필 이름 (선택사항)</summary>
    public string? ProfileName { get; set; }
    
    /// <summary>양방향 매핑 여부</summary>
    public bool IsBidirectional { get; set; }
    
    /// <summary>성능 최적화 레벨</summary>
    public OptimizationLevel OptimizationLevel { get; set; } = OptimizationLevel.Balanced;
}

/// <summary>성능 최적화 레벨</summary>
public enum OptimizationLevel
{
    /// <summary>안전성 우선 - 모든 검증 수행</summary>
    Safe,
    
    /// <summary>균형 - 적절한 성능과 안전성</summary>
    Balanced,
    
    /// <summary>성능 우선 - 최소한의 검증</summary>
    Aggressive
}
```

**사용 예시:**
```csharp
// 기본 매핑
[MapTo(typeof(UserDto))]
public class User { }

// 다중 대상 매핑
[MapTo(typeof(UserDto), IsBidirectional = true)]
[MapTo(typeof(UserSummaryDto), ProfileName = "Summary")]
public class User { }

// 성능 최적화
[MapTo(typeof(UserDto), OptimizationLevel = OptimizationLevel.Aggressive)]
public class User { }
```

### MapPropertyAttribute

속성 레벨에서 세밀한 매핑 제어를 위한 어트리뷰트입니다.

```csharp
namespace FastMapper.Core.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class MapPropertyAttribute : Attribute
{
    /// <summary>기본 생성자</summary>
    public MapPropertyAttribute();
    
    /// <summary>대상 속성명을 지정하여 초기화</summary>
    /// <param name="targetPropertyName">대상 속성명</param>
    public MapPropertyAttribute(string targetPropertyName);
    
    /// <summary>매핑될 대상 속성명</summary>
    public string? TargetPropertyName { get; set; }
    
    /// <summary>매핑에서 제외할지 여부</summary>
    public bool Ignore { get; set; }
    
    /// <summary>커스텀 변환 함수명</summary>
    public string? ConverterMethod { get; set; }
    
    /// <summary>조건부 매핑 함수명</summary>
    public string? ConditionMethod { get; set; }
    
    /// <summary>기본값 (소스가 null일 때 사용)</summary>
    public object? DefaultValue { get; set; }
    
    /// <summary>검증 함수명</summary>
    public string? ValidatorMethod { get; set; }
}
```

**사용 예시:**
```csharp
public class User
{
    // 대상 속성명 변경
    [MapProperty(TargetPropertyName = "FullName")]
    public string Name { get; set; }
    
    // 커스텀 변환
    [MapProperty(ConverterMethod = "FormatEmail")]
    public string Email { get; set; }
    
    // 조건부 매핑
    [MapProperty(ConditionMethod = "ShouldMapAge")]
    public int Age { get; set; }
    
    // 기본값 설정
    [MapProperty(DefaultValue = "Unknown")]
    public string? Country { get; set; }
    
    // 매핑 제외
    [MapProperty(Ignore = true)]
    public string Password { get; set; }
    
    // 커스텀 메서드들
    public static string FormatEmail(string email) => email.ToLowerInvariant();
    public static bool ShouldMapAge(User user) => user.Age >= 0;
}
```

### MapCollectionAttribute

컬렉션 매핑 전용 어트리뷰트입니다.

```csharp
namespace FastMapper.Core.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class MapCollectionAttribute : Attribute
{
    /// <summary>컬렉션 요소 타입</summary>
    public Type? ElementType { get; set; }
    
    /// <summary>컬렉션 타입</summary>
    public CollectionType CollectionType { get; set; } = CollectionType.List;
    
    /// <summary>빈 컬렉션 처리 방식</summary>
    public EmptyCollectionHandling EmptyHandling { get; set; } = EmptyCollectionHandling.CreateEmpty;
    
    /// <summary>중복 제거 여부</summary>
    public bool RemoveDuplicates { get; set; }
}

/// <summary>컬렉션 타입</summary>
public enum CollectionType
{
    List, Array, HashSet, LinkedList, Queue, Stack
}

/// <summary>빈 컬렉션 처리 방식</summary>
public enum EmptyCollectionHandling
{
    CreateEmpty, ReturnNull, ThrowException
}
```

**사용 예시:**
```csharp
public class User
{
    // HashSet으로 변환하며 중복 제거
    [MapCollection(CollectionType = CollectionType.HashSet, RemoveDuplicates = true)]
    public List<string> Tags { get; set; }
    
    // 빈 컬렉션일 때 null 반환
    [MapCollection(EmptyHandling = EmptyCollectionHandling.ReturnNull)]
    public List<Order> Orders { get; set; }
}
```

### MapComplexAttribute

복잡한 객체 매핑을 위한 어트리뷰트입니다.

```csharp
namespace FastMapper.Core.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class MapComplexAttribute : Attribute
{
    /// <summary>중첩 객체 생성 전략</summary>
    public NestedObjectStrategy Strategy { get; set; } = NestedObjectStrategy.CreateNew;
    
    /// <summary>순환 참조 처리 방식</summary>
    public CircularReferenceHandling CircularHandling { get; set; } = CircularReferenceHandling.Ignore;
    
    /// <summary>최대 깊이 제한</summary>
    public int MaxDepth { get; set; } = 10;
}

/// <summary>중첩 객체 생성 전략</summary>
public enum NestedObjectStrategy
{
    CreateNew, ReuseExisting, ShallowCopy
}

/// <summary>순환 참조 처리 방식</summary>
public enum CircularReferenceHandling
{
    Ignore, ThrowException, TrackReferences
}
```

## 🔧 의존성 주입

### ServiceCollectionExtensions

DI 컨테이너에 FastMapper를 등록하는 확장 메서드들입니다.

```csharp
namespace FastMapper.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// FastMapper 서비스들을 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="configuration">구성 설정 (선택사항)</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddFastMapper(
        this IServiceCollection services,
        IConfiguration? configuration = null);
    
    /// <summary>
    /// 특정 어셈블리의 FastMapper 서비스들을 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="assembly">스캔할 어셈블리</param>
    /// <param name="configuration">구성 설정 (선택사항)</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddFastMapper(
        this IServiceCollection services,
        Assembly assembly,
        IConfiguration? configuration = null);
    
    /// <summary>
    /// 여러 어셈블리의 FastMapper 서비스들을 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="assemblies">스캔할 어셈블리들</param>
    /// <param name="configuration">구성 설정 (선택사항)</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddFastMapper(
        this IServiceCollection services,
        Assembly[] assemblies,
        IConfiguration? configuration = null);
    
    /// <summary>
    /// 매핑 옵션을 구성합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="configureOptions">옵션 구성 델리게이트</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection ConfigureFastMapper(
        this IServiceCollection services,
        Action<MappingOptions> configureOptions);
}
```

### IMapperFactory

매퍼 인스턴스를 생성하는 팩토리 인터페이스입니다.

```csharp
namespace FastMapper.Extensions.DependencyInjection;

public interface IMapperFactory
{
    /// <summary>
    /// 매퍼 인스턴스를 생성합니다.
    /// </summary>
    /// <typeparam name="TSource">소스 타입</typeparam>
    /// <typeparam name="TDestination">대상 타입</typeparam>
    /// <returns>매퍼 인스턴스</returns>
    /// <exception cref="InvalidOperationException">매퍼를 찾을 수 없는 경우</exception>
    IMapper<TSource, TDestination> CreateMapper<TSource, TDestination>()
        where TSource : class
        where TDestination : class;
    
    /// <summary>
    /// 양방향 매퍼 인스턴스를 생성합니다.
    /// </summary>
    /// <typeparam name="TFirst">첫 번째 타입</typeparam>
    /// <typeparam name="TSecond">두 번째 타입</typeparam>
    /// <returns>양방향 매퍼 인스턴스 또는 null</returns>
    IBidirectionalMapper<TFirst, TSecond>? CreateBidirectionalMapper<TFirst, TSecond>()
        where TFirst : class
        where TSecond : class;
}
```

## ⚙️ 설정 옵션

### MappingOptions

매핑 동작을 제어하는 옵션 클래스입니다.

```csharp
namespace FastMapper.Core.Common;

public sealed record MappingOptions
{
    /// <summary>null 값 처리 방식</summary>
    public NullValueHandling NullHandling { get; init; } = NullValueHandling.SetNull;
    
    /// <summary>문자열 비교 방식</summary>
    public StringComparison StringComparison { get; init; } = StringComparison.OrdinalIgnoreCase;
    
    /// <summary>최대 중첩 깊이</summary>
    public int MaxDepth { get; init; } = 10;
    
    /// <summary>검증 활성화 여부</summary>
    public bool EnableValidation { get; init; } = true;
    
    /// <summary>성능 모니터링 활성화 여부</summary>
    public bool EnablePerformanceMonitoring { get; init; } = false;
    
    /// <summary>스레드 안전성 보장 여부</summary>
    public bool ThreadSafe { get; init; } = true;
}

/// <summary>null 값 처리 방식</summary>
public enum NullValueHandling
{
    /// <summary>null 값을 그대로 설정</summary>
    SetNull,
    
    /// <summary>null 값을 무시 (기존 값 유지)</summary>
    Ignore,
    
    /// <summary>기본값으로 대체</summary>
    SetDefault
}
```

**사용 예시:**
```csharp
services.ConfigureFastMapper(options =>
{
    options.NullHandling = NullValueHandling.SetDefault;
    options.MaxDepth = 5;
    options.EnableValidation = false;
    options.EnablePerformanceMonitoring = true;
});
```

## 📊 성능 모니터링

### IMappingPerformanceMonitor

매핑 성능을 모니터링하는 인터페이스입니다.

```csharp
namespace FastMapper.Extensions.DependencyInjection;

public interface IMappingPerformanceMonitor
{
    /// <summary>
    /// 매핑 성능을 기록합니다.
    /// </summary>
    /// <param name="mapperName">매퍼 이름</param>
    /// <param name="duration">소요 시간</param>
    /// <param name="itemCount">처리된 항목 수</param>
    void RecordMapping(string mapperName, TimeSpan duration, int itemCount = 1);
    
    /// <summary>
    /// 성능 통계를 조회합니다.
    /// </summary>
    /// <param name="mapperName">매퍼 이름 (null이면 전체 통계)</param>
    /// <returns>성능 통계</returns>
    MappingPerformanceStats GetStatistics(string? mapperName = null);
}

/// <summary>매핑 성능 통계</summary>
public sealed record MappingPerformanceStats(
    string MapperName,
    long TotalMappings,
    TimeSpan TotalDuration,
    TimeSpan AverageDuration,
    TimeSpan MinDuration,
    TimeSpan MaxDuration,
    DateTime LastMappingTime
);
```

**사용 예시:**
```csharp
public class PerformanceService
{
    private readonly IMappingPerformanceMonitor _monitor;
    
    public PerformanceService(IMappingPerformanceMonitor monitor)
    {
        _monitor = monitor;
    }
    
    public void LogPerformanceStats()
    {
        var stats = _monitor.GetStatistics();
        
        Console.WriteLine($"총 매핑: {stats.TotalMappings}회");
        Console.WriteLine($"평균 시간: {stats.AverageDuration.TotalMilliseconds:F2}ms");
        Console.WriteLine($"최대 시간: {stats.MaxDuration.TotalMilliseconds:F2}ms");
    }
}
```

### MappingContext

매핑 과정에서 공유되는 컨텍스트 정보입니다.

```csharp
namespace FastMapper.Core.Common;

public sealed class MappingContext
{
    /// <summary>순환 참조 추적을 위한 객체 캐시</summary>
    public Dictionary<object, object> ObjectCache { get; }
    
    /// <summary>매핑 옵션</summary>
    public MappingOptions Options { get; set; }
    
    /// <summary>사용자 정의 데이터</summary>
    public Dictionary<string, object> UserData { get; }
    
    /// <summary>매핑 통계</summary>
    public MappingStatistics Statistics { get; }
    
    /// <summary>취소 토큰</summary>
    public CancellationToken CancellationToken { get; set; }
    
    /// <summary>순환 참조 확인</summary>
    /// <param name="source">확인할 객체</param>
    /// <returns>순환 참조 여부</returns>
    public bool HasCircularReference(object source);
    
    /// <summary>객체 캐시에 추가</summary>
    /// <param name="source">소스 객체</param>
    /// <param name="destination">대상 객체</param>
    public void AddToCache(object source, object destination);
    
    /// <summary>캐시에서 객체 조회</summary>
    /// <typeparam name="T">조회할 타입</typeparam>
    /// <param name="source">소스 객체</param>
    /// <returns>캐시된 객체 또는 null</returns>
    public T? GetFromCache<T>(object source) where T : class;
}
```

## ⚠️ 예외 처리

### FastMapper 예외들

```csharp
namespace FastMapper.Core.Exceptions;

/// <summary>FastMapper 기본 예외 클래스</summary>
public abstract class FastMapperException : Exception
{
    protected FastMapperException(string message) : base(message) { }
    protected FastMapperException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>매핑 설정 오류 예외</summary>
public sealed class MappingConfigurationException : FastMapperException
{
    public MappingConfigurationException(string message) : base(message) { }
}

/// <summary>매핑 실행 오류 예외</summary>
public sealed class MappingExecutionException : FastMapperException
{
    public MappingExecutionException(string message) : base(message) { }
    public MappingExecutionException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>순환 참조 감지 예외</summary>
public sealed class CircularReferenceException : FastMapperException
{
    public CircularReferenceException(string message) : base(message) { }
}
```

**예외 처리 예시:**
```csharp
try
{
    var result = _mapper.Map(source);
}
catch (ArgumentNullException ex)
{
    // null 인수 처리
    _logger.LogError(ex, "매핑 소스가 null입니다.");
}
catch (MappingExecutionException ex)
{
    // 매핑 실행 오류 처리
    _logger.LogError(ex, "매핑 실행 중 오류가 발생했습니다.");
}
catch (CircularReferenceException ex)
{
    // 순환 참조 처리
    _logger.LogError(ex, "순환 참조가 감지되었습니다.");
}
```

---

**📝 이 API 레퍼런스는 지속적으로 업데이트됩니다. 최신 정보는 [GitHub 리포지토리](https://github.com/fastmapper/fastmapper)에서 확인하세요.**
