using FastMapper.Core.Abstractions;

namespace FastMapper.Extensions.DependencyInjection;

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
