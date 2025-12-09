namespace FastMapper.Core.Common;

/// <summary>
/// null 값 처리 방식
/// </summary>
public enum NullValueHandling
{
    /// <summary>
    /// null 값을 그대로 설정
    /// </summary>
    SetNull,
    
    /// <summary>
    /// null 값을 무시 (기존 값 유지)
    /// </summary>
    Ignore,
    
    /// <summary>
    /// 기본값으로 대체
    /// </summary>
    SetDefault
}
