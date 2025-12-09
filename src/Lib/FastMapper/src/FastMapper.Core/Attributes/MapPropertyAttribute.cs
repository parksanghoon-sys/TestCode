namespace FastMapper.Core.Attributes;

/// <summary>
/// 속성 레벨 매핑 설정 어트리뷰트
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class MapPropertyAttribute : Attribute
{
    /// <summary>
    /// 매핑될 대상 속성명
    /// </summary>
    public string? TargetPropertyName { get; set; }
    
    /// <summary>
    /// 매핑에서 제외할지 여부
    /// </summary>
    public bool Ignore { get; set; }
    
    /// <summary>
    /// 커스텀 변환 함수명
    /// </summary>
    public string? ConverterMethod { get; set; }
    
    /// <summary>
    /// 조건부 매핑 함수명
    /// </summary>
    public string? ConditionMethod { get; set; }
    
    /// <summary>
    /// 기본값 (소스가 null일 때 사용)
    /// </summary>
    public object? DefaultValue { get; set; }
    
    /// <summary>
    /// 검증 함수명
    /// </summary>
    public string? ValidatorMethod { get; set; }

    public MapPropertyAttribute() { }
    
    public MapPropertyAttribute(string targetPropertyName)
    {
        TargetPropertyName = targetPropertyName;
    }
}
