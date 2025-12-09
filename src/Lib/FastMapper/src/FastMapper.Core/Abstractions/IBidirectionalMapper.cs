namespace FastMapper.Core.Abstractions;

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
