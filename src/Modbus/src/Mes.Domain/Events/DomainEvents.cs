using Mes.Domain.Abstractions;
using Mes.Domain.Statuses;
using Mes.Domain.ValueObjects;

namespace Mes.Domain.Events;

/// <summary>
/// 생산 오더 릴리즈 정보가 MES 실행 도메인으로 유입되었음을 나타냅니다.
/// </summary>
public sealed record OrderReleasedIngestedDomainEvent(
    ProductionOrderId OrderId,
    string ItemCode,
    string RouteRevision,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

/// <summary>
/// 공정 실행이 특정 스테이션에서 시작되었음을 나타냅니다.
/// </summary>
public sealed record OperationStartedDomainEvent(
    OperationExecutionId OperationExecutionId,
    ProductionOrderId OrderId,
    StationId StationId,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

/// <summary>
/// 공정 실행이 일시 정지되었음을 나타냅니다.
/// </summary>
public sealed record OperationPausedDomainEvent(
    OperationExecutionId OperationExecutionId,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

/// <summary>
/// 일시 정지된 공정 실행이 다시 재개되었음을 나타냅니다.
/// </summary>
public sealed record OperationResumedDomainEvent(
    OperationExecutionId OperationExecutionId,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

/// <summary>
/// 공정 실행이 정상 수량과 함께 완료되었음을 나타냅니다.
/// </summary>
public sealed record OperationCompletedDomainEvent(
    OperationExecutionId OperationExecutionId,
    MeasuredQuantity GoodQuantity,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

/// <summary>
/// 공정 실행 중 불량 수량이 기록되었음을 나타냅니다.
/// </summary>
public sealed record ScrapRecordedDomainEvent(
    OperationExecutionId OperationExecutionId,
    MeasuredQuantity ScrapQuantity,
    string Reason,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

/// <summary>
/// 자재 Lot의 소모 이력이 기록되었음을 나타냅니다.
/// </summary>
public sealed record MaterialConsumptionRecordedDomainEvent(
    MaterialLotId MaterialLotId,
    WipUnitId WipUnitId,
    MeasuredQuantity ConsumedQuantity,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

/// <summary>
/// 자재 Lot의 반출 또는 반납 이력이 기록되었음을 나타냅니다.
/// </summary>
public sealed record MaterialReturnRecordedDomainEvent(
    MaterialLotId MaterialLotId,
    MeasuredQuantity ReturnedQuantity,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

/// <summary>
/// 자재와 WIP 사이의 genealogy 연결이 생성되었음을 나타냅니다.
/// </summary>
public sealed record GenealogyLinkCreatedDomainEvent(
    MaterialLotId MaterialLotId,
    WipUnitId WipUnitId,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

/// <summary>
/// 도메인 주체가 Hold 상태로 전환되었음을 나타냅니다.
/// </summary>
public sealed record HoldPlacedDomainEvent(
    string SubjectType,
    string SubjectId,
    string Reason,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

/// <summary>
/// 도메인 주체의 Hold 상태가 해제되었음을 나타냅니다.
/// </summary>
public sealed record HoldReleasedDomainEvent(
    string SubjectType,
    string SubjectId,
    string Note,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

/// <summary>
/// 품질 판정 결과가 기록되었음을 나타냅니다.
/// </summary>
public sealed record QualityResultRecordedDomainEvent(
    QualityRecordId QualityRecordId,
    QualityDecisionStatus DecisionStatus,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

/// <summary>
/// 예외 승인 요청이 등록되었음을 나타냅니다.
/// </summary>
public sealed record OverrideRequestedDomainEvent(
    OverrideRequestId OverrideRequestId,
    OperationExecutionId OperationExecutionId,
    string RequestedBy,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

/// <summary>
/// 예외 승인 요청이 승인되었음을 나타냅니다.
/// </summary>
public sealed record OverrideApprovedDomainEvent(
    OverrideRequestId OverrideRequestId,
    string ApprovedBy,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

/// <summary>
/// 예외 승인 요청이 반려되었음을 나타냅니다.
/// </summary>
public sealed record OverrideRejectedDomainEvent(
    OverrideRequestId OverrideRequestId,
    string RejectedBy,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);
