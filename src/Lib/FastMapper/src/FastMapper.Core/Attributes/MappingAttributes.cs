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

/// <summary>
/// 성능 최적화 레벨
/// </summary>
public enum OptimizationLevel
{
    /// <summary>
    /// 기본 매핑 - 안전성 우선
    /// </summary>
    Safe,
    
    /// <summary>
    /// 균형잡힌 최적화
    /// </summary>
    Balanced,
    
    /// <summary>
    /// 최고 성능 - 안전성 검사 최소화
    /// </summary>
    Aggressive
}

/// <summary>
/// 컬렉션 타입
/// </summary>
public enum CollectionType
{
    List,
    Array,
    HashSet,
    LinkedList,
    Queue,
    Stack
}

/// <summary>
/// 빈 컬렉션 처리 방식
/// </summary>
public enum EmptyCollectionHandling
{
    /// <summary>
    /// 빈 컬렉션 생성
    /// </summary>
    CreateEmpty,
    
    /// <summary>
    /// null 반환
    /// </summary>
    ReturnNull,
    
    /// <summary>
    /// 예외 발생
    /// </summary>
    ThrowException
}

/// <summary>
/// 중첩 객체 생성 전략
/// </summary>
public enum NestedObjectStrategy
{
    /// <summary>
    /// 새 객체 생성
    /// </summary>
    CreateNew,
    
    /// <summary>
    /// 기존 객체 재사용 (있는 경우)
    /// </summary>
    ReuseExisting,
    
    /// <summary>
    /// 얕은 복사
    /// </summary>
    ShallowCopy
}

/// <summary>
/// 순환 참조 처리 방식
/// </summary>
public enum CircularReferenceHandling
{
    /// <summary>
    /// 무시 (null 할당)
    /// </summary>
    Ignore,
    
    /// <summary>
    /// 예외 발생
    /// </summary>
    ThrowException,
    
    /// <summary>
    /// 참조 추적으로 해결
    /// </summary>
    TrackReferences
}
