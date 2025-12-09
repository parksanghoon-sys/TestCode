namespace FastMapper.Core.Attributes;

/// <summary>
/// 클래스에 적용하여 매핑 대상임을 표시하는 어트리뷰트
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class MapToAttribute : Attribute
{
    /// <summary>
    /// 매핑 대상 타입
    /// </summary>
    public Type TargetType { get; }
    
    /// <summary>
    /// 매핑 프로필 이름 (선택사항)
    /// </summary>
    public string? ProfileName { get; set; }
    
    /// <summary>
    /// 양방향 매핑 여부
    /// </summary>
    public bool IsBidirectional { get; set; }
    
    /// <summary>
    /// 성능 최적화 레벨
    /// </summary>
    public OptimizationLevel OptimizationLevel { get; set; } = OptimizationLevel.Balanced;

    public MapToAttribute(Type targetType)
    {
        TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
    }
}
