namespace FastMapper.Extensions.DependencyInjection;

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
