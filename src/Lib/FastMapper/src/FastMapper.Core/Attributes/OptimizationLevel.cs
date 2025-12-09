namespace FastMapper.Core.Attributes;

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
