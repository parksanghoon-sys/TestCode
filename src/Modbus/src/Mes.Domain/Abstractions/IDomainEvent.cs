namespace Mes.Domain.Abstractions;

/// <summary>
/// 도메인 이벤트가 제공해야 하는 최소 계약을 정의합니다.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// 이벤트가 발생한 시각입니다.
    /// </summary>
    DateTimeOffset OccurredAt { get; }
}
