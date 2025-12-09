using FastMapper.Core.Abstractions;
using FastMapper.Core.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
