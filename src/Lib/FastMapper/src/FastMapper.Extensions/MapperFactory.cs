using FastMapper.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FastMapper.Extensions.DependencyInjection;

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
