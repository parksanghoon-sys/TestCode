using Mes.Domain.Abstractions;
using Mes.Domain.Common;
using Mes.Domain.Events;
using Mes.Domain.Statuses;
using Mes.Domain.ValueObjects;

namespace Mes.Domain.Aggregates;

/// <summary>
/// 개별 공정 단위의 실행 상태와 실적을 관리하는 애그리거트입니다.
/// </summary>
public sealed class OperationExecution : AggregateRoot<OperationExecutionId>
{
    private OperationExecutionStatus? _statusBeforeHold;

    /// <summary>
    /// 공정 실행 애그리거트를 초기화합니다.
    /// </summary>
    /// <param name="id">공정 실행 식별자입니다.</param>
    /// <param name="productionOrderId">상위 생산 오더 식별자입니다.</param>
    /// <param name="operationSequence">공정 순번입니다.</param>
    /// <param name="quantityUnit">실적 수량 단위입니다.</param>
    private OperationExecution(
        OperationExecutionId id,
        ProductionOrderId productionOrderId,
        int operationSequence,
        string quantityUnit) : base(id)
    {
        ProductionOrderId = productionOrderId;
        OperationSequence = DomainGuard.Positive(operationSequence, nameof(operationSequence));
        QuantityUnit = DomainGuard.NotWhiteSpace(quantityUnit, nameof(quantityUnit)).ToUpperInvariant();
        Status = OperationExecutionStatus.Ready;
        GoodQuantity = MeasuredQuantity.Zero(QuantityUnit);
        ScrapQuantity = MeasuredQuantity.Zero(QuantityUnit);
    }

    public ProductionOrderId ProductionOrderId { get; }

    public int OperationSequence { get; }

    public string QuantityUnit { get; }

    public OperationExecutionStatus Status { get; private set; }

    public StationId? StationId { get; private set; }

    public string? HoldReason { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public MeasuredQuantity GoodQuantity { get; private set; }

    public MeasuredQuantity ScrapQuantity { get; private set; }

    /// <summary>
    /// 새로운 공정 실행 애그리거트를 생성합니다.
    /// </summary>
    /// <param name="id">공정 실행 식별자입니다.</param>
    /// <param name="productionOrderId">상위 생산 오더 식별자입니다.</param>
    /// <param name="operationSequence">공정 순번입니다.</param>
    /// <param name="quantityUnit">실적 수량 단위입니다.</param>
    /// <returns>초기 상태의 공정 실행 애그리거트입니다.</returns>
    public static OperationExecution Create(
        OperationExecutionId id,
        ProductionOrderId productionOrderId,
        int operationSequence,
        string quantityUnit = "EA")
    {
        return new OperationExecution(id, productionOrderId, operationSequence, quantityUnit);
    }

    /// <summary>
    /// 공정 실행을 작업 대기 상태로 전환합니다.
    /// </summary>
    public void QueueForExecution()
    {
        DomainGuard.Against(Status != OperationExecutionStatus.Ready, "Only a ready operation can be queued.");
        Status = OperationExecutionStatus.Queued;
    }

    /// <summary>
    /// 공정 실행을 지정한 스테이션에서 시작합니다.
    /// </summary>
    /// <param name="stationId">작업 스테이션 식별자입니다.</param>
    /// <param name="occurredAt">시작 시각입니다.</param>
    public void Start(StationId stationId, DateTimeOffset occurredAt)
    {
        DomainGuard.Against(Status is not (OperationExecutionStatus.Ready or OperationExecutionStatus.Queued), "Only a ready or queued operation can start.");

        StationId = stationId;
        StartedAt ??= occurredAt;
        Status = OperationExecutionStatus.Running;
        Raise(new OperationStartedDomainEvent(Id, ProductionOrderId, stationId, occurredAt));
    }

    /// <summary>
    /// 실행 중인 공정을 일시 정지합니다.
    /// </summary>
    /// <param name="occurredAt">정지 시각입니다.</param>
    public void Pause(DateTimeOffset occurredAt)
    {
        DomainGuard.Against(Status != OperationExecutionStatus.Running, "Only a running operation can be paused.");
        Status = OperationExecutionStatus.Paused;
        Raise(new OperationPausedDomainEvent(Id, occurredAt));
    }

    /// <summary>
    /// 일시 정지된 공정을 재개합니다.
    /// </summary>
    /// <param name="occurredAt">재개 시각입니다.</param>
    public void Resume(DateTimeOffset occurredAt)
    {
        DomainGuard.Against(Status != OperationExecutionStatus.Paused, "Only a paused operation can be resumed.");
        Status = OperationExecutionStatus.Running;
        Raise(new OperationResumedDomainEvent(Id, occurredAt));
    }

    /// <summary>
    /// 공정 실행을 Hold 상태로 전환합니다.
    /// </summary>
    /// <param name="reason">Hold 사유입니다.</param>
    /// <param name="occurredAt">Hold 적용 시각입니다.</param>
    public void PlaceHold(string reason, DateTimeOffset occurredAt)
    {
        DomainGuard.Against(Status is OperationExecutionStatus.Done or OperationExecutionStatus.Aborted or OperationExecutionStatus.Hold, "Operation cannot be placed on hold from its current state.");

        HoldReason = DomainGuard.NotWhiteSpace(reason, nameof(reason));
        _statusBeforeHold = Status;
        Status = OperationExecutionStatus.Hold;

        Raise(new HoldPlacedDomainEvent(nameof(OperationExecution), Id.ToString(), HoldReason, occurredAt));
    }

    /// <summary>
    /// Hold 상태의 공정 실행을 이전 작업 상태로 복귀시킵니다.
    /// </summary>
    /// <param name="note">해제 메모입니다.</param>
    /// <param name="occurredAt">해제 시각입니다.</param>
    public void ReleaseHold(string note, DateTimeOffset occurredAt)
    {
        DomainGuard.Against(Status != OperationExecutionStatus.Hold, "Only a held operation can be released.");

        HoldReason = null;
        Status = _statusBeforeHold ?? OperationExecutionStatus.Queued;
        _statusBeforeHold = null;

        Raise(new HoldReleasedDomainEvent(nameof(OperationExecution), Id.ToString(), DomainGuard.NotWhiteSpace(note, nameof(note)), occurredAt));
    }

    /// <summary>
    /// 공정 실행 중 발생한 불량 수량을 기록합니다.
    /// </summary>
    /// <param name="scrapQuantity">기록할 불량 수량입니다.</param>
    /// <param name="reason">불량 사유입니다.</param>
    /// <param name="occurredAt">기록 시각입니다.</param>
    public void RecordScrap(MeasuredQuantity scrapQuantity, string reason, DateTimeOffset occurredAt)
    {
        EnsureQuantityUnit(scrapQuantity);
        DomainGuard.Against(Status is not (OperationExecutionStatus.Running or OperationExecutionStatus.Paused or OperationExecutionStatus.Rework), "Scrap can only be recorded while the operation is active.");

        ScrapQuantity = ScrapQuantity.Add(scrapQuantity);
        Raise(new ScrapRecordedDomainEvent(Id, scrapQuantity, DomainGuard.NotWhiteSpace(reason, nameof(reason)), occurredAt));
    }

    /// <summary>
    /// 정상 수량을 반영하며 공정 실행을 완료합니다.
    /// </summary>
    /// <param name="goodQuantity">완료로 반영할 정상 수량입니다.</param>
    /// <param name="occurredAt">완료 시각입니다.</param>
    public void Complete(MeasuredQuantity goodQuantity, DateTimeOffset occurredAt)
    {
        EnsureQuantityUnit(goodQuantity);
        DomainGuard.Against(Status is not (OperationExecutionStatus.Running or OperationExecutionStatus.Rework), "Only a running or rework operation can complete.");

        GoodQuantity = GoodQuantity.Add(goodQuantity);
        CompletedAt = occurredAt;
        Status = OperationExecutionStatus.Done;

        Raise(new OperationCompletedDomainEvent(Id, goodQuantity, occurredAt));
    }

    /// <summary>
    /// 입력 수량 단위가 공정 수량 단위와 일치하는지 확인합니다.
    /// </summary>
    /// <param name="quantity">검증할 수량입니다.</param>
    private void EnsureQuantityUnit(MeasuredQuantity quantity)
    {
        DomainGuard.Against(!string.Equals(QuantityUnit, quantity.Unit, StringComparison.OrdinalIgnoreCase), "Quantity unit does not match the operation unit.");
    }
}
