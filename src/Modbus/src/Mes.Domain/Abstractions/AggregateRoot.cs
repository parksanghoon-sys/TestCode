namespace Mes.Domain.Abstractions;

/// <summary>
/// 도메인 이벤트를 수집하는 애그리거트 루트 기본 클래스를 제공합니다.
/// </summary>
/// <typeparam name="TId">애그리거트 식별자 형식입니다.</typeparam>
public abstract class AggregateRoot<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// 지정한 식별자로 애그리거트 루트를 초기화합니다.
    /// </summary>
    /// <param name="id">애그리거트 식별자입니다.</param>
    protected AggregateRoot(TId id)
    {
        Id = id;
    }

    public TId Id { get; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// 새 도메인 이벤트를 애그리거트 이벤트 목록에 추가합니다.
    /// </summary>
    /// <param name="domainEvent">추가할 도메인 이벤트입니다.</param>
    protected void Raise(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// 현재 애그리거트에 누적된 도메인 이벤트를 모두 비웁니다.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
