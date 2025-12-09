namespace FastMapper.Core.Attributes;

/// <summary>
/// 컬렉션 매핑 전용 어트리뷰트
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class MapCollectionAttribute : Attribute
{
    /// <summary>
    /// 컬렉션 요소 타입
    /// </summary>
    public Type? ElementType { get; set; }
    
    /// <summary>
    /// 컬렉션 타입 (List, Array, HashSet 등)
    /// </summary>
    public CollectionType CollectionType { get; set; } = CollectionType.List;
    
    /// <summary>
    /// 빈 컬렉션 처리 방식
    /// </summary>
    public EmptyCollectionHandling EmptyHandling { get; set; } = EmptyCollectionHandling.CreateEmpty;
    
    /// <summary>
    /// 중복 제거 여부 (HashSet 등에서 사용)
    /// </summary>
    public bool RemoveDuplicates { get; set; }
}
