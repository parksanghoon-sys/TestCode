namespace FastMapper.Core.Abstractions;

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
