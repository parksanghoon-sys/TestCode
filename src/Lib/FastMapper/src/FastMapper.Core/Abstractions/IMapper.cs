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

/// <summary>
/// 양방향 매퍼 인터페이스
/// </summary>
/// <typeparam name="TFirst">첫 번째 타입</typeparam>
/// <typeparam name="TSecond">두 번째 타입</typeparam>
public interface IBidirectionalMapper<TFirst, TSecond> 
    where TFirst : class 
    where TSecond : class
{
    /// <summary>
    /// 첫 번째 타입에서 두 번째 타입으로 매핑
    /// </summary>
    TSecond MapTo(TFirst source);
    
    /// <summary>
    /// 두 번째 타입에서 첫 번째 타입으로 매핑
    /// </summary>
    TFirst MapFrom(TSecond source);
}

/// <summary>
/// 동적 매퍼 인터페이스 - 런타임에 타입을 결정
/// </summary>
public interface IDynamicMapper
{
    /// <summary>
    /// 동적 타입 매핑
    /// </summary>
    /// <typeparam name="T">대상 타입</typeparam>
    /// <param name="source">소스 객체</param>
    /// <returns>변환된 객체</returns>
    T? Map<T>(object source) where T : class;
    
    /// <summary>
    /// 타입 기반 매핑 지원 여부 확인
    /// </summary>
    /// <param name="sourceType">소스 타입</param>
    /// <param name="destinationType">대상 타입</param>
    /// <returns>지원 여부</returns>
    bool CanMap(Type sourceType, Type destinationType);
}
