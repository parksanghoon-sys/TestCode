namespace FastMapper.Core.Attributes;

/// <summary>
/// 복잡한 객체 매핑을 위한 어트리뷰트
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class MapComplexAttribute : Attribute
{
    /// <summary>
    /// 중첩 객체 생성 전략
    /// </summary>
    public NestedObjectStrategy Strategy { get; set; } = NestedObjectStrategy.CreateNew;
    
    /// <summary>
    /// 순환 참조 처리 방식
    /// </summary>
    public CircularReferenceHandling CircularHandling { get; set; } = CircularReferenceHandling.Ignore;
    
    /// <summary>
    /// 최대 깊이 제한
    /// </summary>
    public int MaxDepth { get; set; } = 10;
}
