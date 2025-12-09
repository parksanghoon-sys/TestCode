using FastMapper.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace FastMapper.Extensions.DependencyInjection;

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
