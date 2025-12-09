using System.Linq.Expressions;

namespace FastMapper.Extensions.DependencyInjection;

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
