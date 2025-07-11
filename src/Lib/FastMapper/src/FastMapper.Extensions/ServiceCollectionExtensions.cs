using FastMapper.Core.Abstractions;
using FastMapper.Core.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace FastMapper.Extensions.DependencyInjection;

/// <summary>
/// FastMapper 의존성 주입 확장 메서드
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// FastMapper 서비스들을 DI 컨테이너에 등록
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="configuration">구성 설정 (선택사항)</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddFastMapper(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        return services.AddFastMapper(Assembly.GetCallingAssembly(), configuration);
    }

    /// <summary>
    /// 특정 어셈블리의 FastMapper 서비스들을 DI 컨테이너에 등록
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="assembly">스캔할 어셈블리</param>
    /// <param name="configuration">구성 설정 (선택사항)</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddFastMapper(
        this IServiceCollection services,
        Assembly assembly,
        IConfiguration? configuration = null)
    {
        return services.AddFastMapper(new[] { assembly }, configuration);
    }

    /// <summary>
    /// 여러 어셈블리의 FastMapper 서비스들을 DI 컨테이너에 등록
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="assemblies">스캔할 어셈블리들</param>
    /// <param name="configuration">구성 설정 (선택사항)</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddFastMapper(
        this IServiceCollection services,
        Assembly[] assemblies,
        IConfiguration? configuration = null)
    {
        // 매핑 옵션 구성
        var options = new MappingOptions();
        configuration?.GetSection("FastMapper").Bind(options);
        
        services.TryAddSingleton(options);

        // 매퍼 팩토리 등록
        services.TryAddSingleton<IMapperFactory, MapperFactory>();
        
        // 동적 매퍼 등록
        services.TryAddSingleton<IDynamicMapper, DynamicMapper>();

        // 어셈블리에서 생성된 매퍼들을 스캔하여 등록
        RegisterGeneratedMappers(services, assemblies);

        // 매핑 프로필 등록 (사용자 정의 매핑 규칙)
        RegisterMappingProfiles(services, assemblies);

        // 성능 모니터링 서비스 등록 (옵션)
        if (options.EnablePerformanceMonitoring)
        {
            services.TryAddSingleton<IMappingPerformanceMonitor, MappingPerformanceMonitor>();
        }

        return services;
    }

    /// <summary>
    /// 매핑 프로필 기반 구성
    /// </summary>
    /// <typeparam name="TProfile">매핑 프로필 타입</typeparam>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddFastMapperProfile<TProfile>(this IServiceCollection services)
        where TProfile : class, IMappingProfile
    {
        services.TryAddSingleton<IMappingProfile, TProfile>();
        return services;
    }

    /// <summary>
    /// 커스텀 매핑 설정
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="configureOptions">옵션 구성 델리게이트</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection ConfigureFastMapper(
        this IServiceCollection services,
        Action<MappingOptions> configureOptions)
    {
        services.Configure(configureOptions);
        return services;
    }

    /// <summary>
    /// 생성된 매퍼들을 스캔하여 DI에 등록
    /// </summary>
    private static void RegisterGeneratedMappers(IServiceCollection services, Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            // Generated 네임스페이스에서 매퍼 클래스들을 찾아 등록
            var mapperTypes = assembly.GetTypes()
                .Where(type => type.Namespace?.Contains(".Generated") == true)
                .Where(type => type.Name.EndsWith("Mapper"))
                .Where(type => !type.IsAbstract && !type.IsInterface);

            foreach (var mapperType in mapperTypes)
            {
                // IMapper<,> 인터페이스 구현 확인 및 등록
                var mapperInterfaces = mapperType.GetInterfaces()
                    .Where(i => i.IsGenericType && 
                               (i.GetGenericTypeDefinition() == typeof(IMapper<,>) ||
                                i.GetGenericTypeDefinition() == typeof(IBidirectionalMapper<,>)));

                foreach (var mapperInterface in mapperInterfaces)
                {
                    services.TryAddScoped(mapperInterface, mapperType);
                }

                // 구체 타입으로도 등록 (직접 사용 가능)
                services.TryAddScoped(mapperType);
            }
        }
    }

    /// <summary>
    /// 매핑 프로필들을 스캔하여 등록
    /// </summary>
    private static void RegisterMappingProfiles(IServiceCollection services, Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var profileTypes = assembly.GetTypes()
                .Where(type => typeof(IMappingProfile).IsAssignableFrom(type))
                .Where(type => !type.IsAbstract && !type.IsInterface);

            foreach (var profileType in profileTypes)
            {
                services.TryAddSingleton(typeof(IMappingProfile), profileType);
            }
        }
    }
}

/// <summary>
/// 매퍼 팩토리 인터페이스
/// </summary>
public interface IMapperFactory
{
    /// <summary>
    /// 매퍼 인스턴스 생성
    /// </summary>
    /// <typeparam name="TSource">소스 타입</typeparam>
    /// <typeparam name="TDestination">대상 타입</typeparam>
    /// <returns>매퍼 인스턴스</returns>
    IMapper<TSource, TDestination> CreateMapper<TSource, TDestination>()
        where TSource : class
        where TDestination : class;

    /// <summary>
    /// 양방향 매퍼 인스턴스 생성
    /// </summary>
    /// <typeparam name="TFirst">첫 번째 타입</typeparam>
    /// <typeparam name="TSecond">두 번째 타입</typeparam>
    /// <returns>양방향 매퍼 인스턴스</returns>
    IBidirectionalMapper<TFirst, TSecond>? CreateBidirectionalMapper<TFirst, TSecond>()
        where TFirst : class
        where TSecond : class;
}

/// <summary>
/// 매퍼 팩토리 구현
/// </summary>
internal sealed class MapperFactory : IMapperFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MapperFactory> _logger;

    public MapperFactory(IServiceProvider serviceProvider, ILogger<MapperFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public IMapper<TSource, TDestination> CreateMapper<TSource, TDestination>()
        where TSource : class
        where TDestination : class
    {
        var mapper = _serviceProvider.GetService<IMapper<TSource, TDestination>>();
        
        if (mapper is null)
        {
            _logger.LogWarning("매퍼를 찾을 수 없습니다: {SourceType} -> {DestinationType}", 
                typeof(TSource).Name, typeof(TDestination).Name);
            
            throw new InvalidOperationException(
                $"매퍼를 찾을 수 없습니다: {typeof(TSource).Name} -> {typeof(TDestination).Name}. " +
                "MapTo 어트리뷰트가 적용되었는지 확인하세요.");
        }

        return mapper;
    }

    public IBidirectionalMapper<TFirst, TSecond>? CreateBidirectionalMapper<TFirst, TSecond>()
        where TFirst : class
        where TSecond : class
    {
        return _serviceProvider.GetService<IBidirectionalMapper<TFirst, TSecond>>();
    }
}

/// <summary>
/// 동적 매퍼 구현
/// </summary>
internal sealed class DynamicMapper : IDynamicMapper
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DynamicMapper> _logger;

    public DynamicMapper(IServiceProvider serviceProvider, ILogger<DynamicMapper> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public T? Map<T>(object source) where T : class
    {
        if (source is null) return null;

        var sourceType = source.GetType();
        var destinationType = typeof(T);

        // 제네릭 매퍼 타입 생성
        var mapperType = typeof(IMapper<,>).MakeGenericType(sourceType, destinationType);
        var mapper = _serviceProvider.GetService(mapperType);

        if (mapper is null)
        {
            _logger.LogWarning("동적 매퍼를 찾을 수 없습니다: {SourceType} -> {DestinationType}", 
                sourceType.Name, destinationType.Name);
            return null;
        }

        // 리플렉션을 통해 Map 메서드 호출
        var mapMethod = mapperType.GetMethod("Map");
        return mapMethod?.Invoke(mapper, new[] { source }) as T;
    }

    public bool CanMap(Type sourceType, Type destinationType)
    {
        var mapperType = typeof(IMapper<,>).MakeGenericType(sourceType, destinationType);
        return _serviceProvider.GetService(mapperType) is not null;
    }
}

/// <summary>
/// 매핑 프로필 인터페이스 - 커스텀 매핑 규칙 정의
/// </summary>
public interface IMappingProfile
{
    /// <summary>
    /// 매핑 규칙 구성
    /// </summary>
    /// <param name="configuration">매핑 구성</param>
    void Configure(IMappingConfiguration configuration);
}

/// <summary>
/// 매핑 구성 인터페이스
/// </summary>
public interface IMappingConfiguration
{
    /// <summary>
    /// 커스텀 매핑 규칙 생성
    /// </summary>
    /// <typeparam name="TSource">소스 타입</typeparam>
    /// <typeparam name="TDestination">대상 타입</typeparam>
    /// <returns>매핑 빌더</returns>
    IMappingBuilder<TSource, TDestination> CreateMap<TSource, TDestination>()
        where TSource : class
        where TDestination : class;
}

/// <summary>
/// 매핑 빌더 인터페이스
/// </summary>
public interface IMappingBuilder<TSource, TDestination>
    where TSource : class
    where TDestination : class
{
    /// <summary>
    /// 속성 매핑 규칙 정의
    /// </summary>
    /// <typeparam name="TMember">속성 타입</typeparam>
    /// <param name="destinationMember">대상 속성 선택자</param>
    /// <returns>속성 매핑 빌더</returns>
    IPropertyMappingBuilder<TSource, TDestination, TMember> ForMember<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember);

    /// <summary>
    /// 역방향 매핑 설정
    /// </summary>
    /// <returns>매핑 빌더</returns>
    IMappingBuilder<TDestination, TSource> ReverseMap();
}

/// <summary>
/// 속성 매핑 빌더 인터페이스
/// </summary>
public interface IPropertyMappingBuilder<TSource, TDestination, TMember>
    where TSource : class
    where TDestination : class
{
    /// <summary>
    /// 소스 속성 매핑
    /// </summary>
    /// <param name="sourceMember">소스 속성 선택자</param>
    /// <returns>매핑 빌더</returns>
    IMappingBuilder<TSource, TDestination> MapFrom<TSourceMember>(
        Expression<Func<TSource, TSourceMember>> sourceMember);

    /// <summary>
    /// 커스텀 변환 함수 사용
    /// </summary>
    /// <param name="converter">변환 함수</param>
    /// <returns>매핑 빌더</returns>
    IMappingBuilder<TSource, TDestination> ConvertUsing(
        Func<TSource, TMember> converter);

    /// <summary>
    /// 매핑 무시
    /// </summary>
    /// <returns>매핑 빌더</returns>
    IMappingBuilder<TSource, TDestination> Ignore();

    /// <summary>
    /// 조건부 매핑
    /// </summary>
    /// <param name="condition">조건 함수</param>
    /// <returns>매핑 빌더</returns>
    IMappingBuilder<TSource, TDestination> Condition(
        Func<TSource, bool> condition);
}

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
