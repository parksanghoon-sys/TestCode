namespace Mes.Domain.Abstractions;

/// <summary>
/// 발생 시각을 포함하는 도메인 이벤트 기본 레코드입니다.
/// </summary>
/// <param name="OccurredAt">이벤트가 발생한 시각입니다.</param>
public abstract record DomainEvent(DateTimeOffset OccurredAt) : IDomainEvent;
