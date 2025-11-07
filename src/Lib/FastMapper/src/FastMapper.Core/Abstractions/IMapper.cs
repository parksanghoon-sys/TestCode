namespace FastMapper.Core.Abstractions;

/// <summary>
/// 범용 매퍼 인터페이스 - DI 컨테이너에서 주입받아 사용
/// </summary>
/// <typeparam name="TSource">소스 타입</typeparam>
/// <typeparam name="TDestination">대상 타입</typeparam>
public interface IMapper<in TSource, TDestination> 
    where TSource : class 
    where TDestination : class
{
    /// <summary>
    /// 단일 객체 매핑
    /// </summary>
    /// <param name="source">소스 객체</param>
    /// <returns>변환된 대상 객체</returns>
    TDestination Map(TSource source);
    
    /// <summary>
    /// 컬렉션 매핑 - 고성능 배치 처리
    /// </summary>
    /// <param name="sources">소스 컬렉션</param>
    /// <returns>변환된 대상 컬렉션</returns>
    IEnumerable<TDestination> MapCollection(IEnumerable<TSource> sources);
    
    /// <summary>
    /// 비동기 컬렉션 매핑
    /// </summary>
    /// <param name="sources">소스 컬렉션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>변환된 대상 컬렉션</returns>
    Task<IReadOnlyList<TDestination>> MapCollectionAsync(
        IEnumerable<TSource> sources, 
        CancellationToken cancellationToken = default);
}
