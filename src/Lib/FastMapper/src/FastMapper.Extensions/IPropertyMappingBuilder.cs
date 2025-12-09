using System.Linq.Expressions;

namespace FastMapper.Extensions.DependencyInjection;

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
