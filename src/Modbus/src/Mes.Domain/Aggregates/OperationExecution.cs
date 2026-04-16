using Mes.Domain.Abstractions;
using Mes.Domain.Common;
using Mes.Domain.Events;
using Mes.Domain.Statuses;
using Mes.Domain.ValueObjects;

namespace Mes.Domain.Aggregates;

/// <summary>
/// 개별 공정 실행의 수명주기와 hold 게이트 상태를 관리하는 aggregate입니다.
/// </summary>
public sealed class OperationExecution : AggregateRoot<OperationExecutionId>
{
    /// <summary>
    /// 공정 실행 aggregate를 초기화합니다.
    /// </summary>
    /// <param name="id">공정 실행 식별자입니다.</param>
    /// <param name="productionOrderId">상위 생산 오더 식별자입니다.</param>
    /// <param name="operationSequence">공정 순번입니다.</param>
    /// <param name="quantityUnit">수량 단위입니다.</param>
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

    /// <summary>
    /// 상위 생산 오더 식별자입니다.
    /// </summary>
    public ProductionOrderId ProductionOrderId { get; }

    /// <summary>
    /// 공정 순번입니다.
    /// </summary>
    public int OperationSequence { get; }

    /// <summary>
    /// 수량 단위입니다.
    /// </summary>
    public string QuantityUnit { get; }

    /// <summary>
    /// 현재 공정 실행 상태입니다.
    /// </summary>
    public OperationExecutionStatus Status { get; private set; }

    /// <summary>
    /// 작업이 배정된 스테이션 식별자입니다.
    /// </summary>
    public StationId? StationId { get; private set; }

    /// <summary>
    /// 현재 hold 사유입니다.
    /// </summary>
    public string? HoldReason { get; private set; }

    /// <summary>
    /// hold 진입 직전 상태입니다.
    /// </summary>
    public OperationExecutionStatus? StatusBeforeHold { get; private set; }

    /// <summary>
    /// 현재 hold 출처 유형입니다.
    /// </summary>
    public string? HoldSourceType { get; private set; }

    /// <summary>
    /// 현재 hold 출처 식별자입니다.
    /// </summary>
    public string? HoldSourceId { get; private set; }

    /// <summary>
    /// 시작 시각입니다.
    /// </summary>
    public DateTimeOffset? StartedAt { get; private set; }

    /// <summary>
    /// 완료 시각입니다.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>
    /// 누적 양품 수량입니다.
    /// </summary>
    public MeasuredQuantity GoodQuantity { get; private set; }

    /// <summary>
    /// 누적 불량 수량입니다.
    /// </summary>
    public MeasuredQuantity ScrapQuantity { get; private set; }

    /// <summary>
    /// 새로운 공정 실행 aggregate를 생성합니다.
    /// </summary>
    /// <param name="id">공정 실행 식별자입니다.</param>
    /// <param name="productionOrderId">상위 생산 오더 식별자입니다.</param>
    /// <param name="operationSequence">공정 순번입니다.</param>
    /// <param name="quantityUnit">수량 단위입니다.</param>
    /// <returns>초기 상태의 공정 실행 aggregate입니다.</returns>
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
    /// 공정 실행을 단순 hold 상태로 전환합니다.
    /// </summary>
    /// <param name="reason">hold 사유입니다.</param>
    /// <param name="occurredAt">hold 시각입니다.</param>
    public void PlaceHold(string reason, DateTimeOffset occurredAt)
    {
        PlaceHold(new OperationHoldRequest(reason, occurredAt));
    }

    /// <summary>
    /// 공정 실행을 provenance를 포함한 hold 상태로 전환합니다.
    /// </summary>
    /// <param name="request">hold 적용 요청입니다.</param>
    public void PlaceHold(OperationHoldRequest request)
    {
        DomainGuard.Against(Status is OperationExecutionStatus.Done or OperationExecutionStatus.Aborted or OperationExecutionStatus.Hold, "Operation cannot be placed on hold from its current state.");

        HoldReason = DomainGuard.NotWhiteSpace(request.Reason, nameof(request.Reason));
        StatusBeforeHold = Status;
        HoldSourceType = NormalizeOptional(request.SourceType);
        HoldSourceId = NormalizeOptional(request.SourceId);
        Status = OperationExecutionStatus.Hold;

        Raise(new HoldPlacedDomainEvent(nameof(OperationExecution), Id.ToString(), HoldReason, request.OccurredAt));
    }

    /// <summary>
    /// 단순 hold 해제를 수행합니다.
    /// </summary>
    /// <param name="note">해제 메모입니다.</param>
    /// <param name="occurredAt">해제 시각입니다.</param>
    public void ReleaseHold(string note, DateTimeOffset occurredAt)
    {
        ReleaseHold(new OperationHoldReleaseRequest(note, occurredAt));
    }

    /// <summary>
    /// hold 출처 검증을 포함한 hold 해제를 수행합니다.
    /// </summary>
    /// <param name="request">hold 해제 요청입니다.</param>
    public void ReleaseHold(OperationHoldReleaseRequest request)
    {
        DomainGuard.Against(Status != OperationExecutionStatus.Hold, "Only a held operation can be released.");
        EnsureMatchingHoldSource(request);

        HoldReason = null;
        Status = StatusBeforeHold ?? OperationExecutionStatus.Queued;
        StatusBeforeHold = null;
        HoldSourceType = null;
        HoldSourceId = null;

        Raise(new HoldReleasedDomainEvent(nameof(OperationExecution), Id.ToString(), DomainGuard.NotWhiteSpace(request.Note, nameof(request.Note)), request.OccurredAt));
    }

    /// <summary>
    /// 현재 hold가 지정한 출처와 일치하는지 확인합니다.
    /// </summary>
    /// <param name="sourceType">비교할 hold 출처 유형입니다.</param>
    /// <param name="sourceId">비교할 hold 출처 식별자입니다.</param>
    /// <returns>현재 hold 출처가 일치하면 <see langword="true"/>입니다.</returns>
    public bool IsHeldBy(string sourceType, string sourceId)
    {
        return Status == OperationExecutionStatus.Hold
            && string.Equals(HoldSourceType, DomainGuard.NotWhiteSpace(sourceType, nameof(sourceType)), StringComparison.OrdinalIgnoreCase)
            && string.Equals(HoldSourceId, DomainGuard.NotWhiteSpace(sourceId, nameof(sourceId)), StringComparison.Ordinal);
    }

    /// <summary>
    /// 공정 실행 중 기록된 불량 수량을 누적합니다.
    /// </summary>
    /// <param name="scrapQuantity">추가할 불량 수량입니다.</param>
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
    /// 양품 수량을 반영하고 공정 실행을 완료합니다.
    /// </summary>
    /// <param name="goodQuantity">완료 시 반영할 양품 수량입니다.</param>
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
    /// hold 해제 요청의 출처가 현재 hold provenance와 일치하는지 검증합니다.
    /// </summary>
    /// <param name="request">검증할 hold 해제 요청입니다.</param>
    private void EnsureMatchingHoldSource(OperationHoldReleaseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ExpectedSourceType) && string.IsNullOrWhiteSpace(request.ExpectedSourceId))
        {
            return;
        }

        var expectedSourceType = NormalizeOptional(request.ExpectedSourceType);
        var expectedSourceId = NormalizeOptional(request.ExpectedSourceId);

        DomainGuard.Against(!string.Equals(HoldSourceType, expectedSourceType, StringComparison.OrdinalIgnoreCase), "Hold source type does not match the current operation hold.");
        DomainGuard.Against(!string.Equals(HoldSourceId, expectedSourceId, StringComparison.Ordinal), "Hold source id does not match the current operation hold.");
    }

    /// <summary>
    /// 선택 입력값을 정규화하여 비교와 저장에 사용할 형태로 맞춥니다.
    /// </summary>
    /// <param name="value">정규화할 입력값입니다.</param>
    /// <returns>정규화된 값 또는 <see langword="null"/>입니다.</returns>
    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    /// <summary>
    /// 입력 수량 단위가 공정 단위와 일치하는지 확인합니다.
    /// </summary>
    /// <param name="quantity">검증할 수량입니다.</param>
    private void EnsureQuantityUnit(MeasuredQuantity quantity)
    {
        DomainGuard.Against(!string.Equals(QuantityUnit, quantity.Unit, StringComparison.OrdinalIgnoreCase), "Quantity unit does not match the operation unit.");
    }
}

/// <summary>
/// 공정 hold 적용에 필요한 입력값 묶음입니다.
/// </summary>
/// <param name="Reason">hold 사유입니다.</param>
/// <param name="OccurredAt">hold 시각입니다.</param>
/// <param name="SourceType">hold 출처 유형입니다.</param>
/// <param name="SourceId">hold 출처 식별자입니다.</param>
public sealed record OperationHoldRequest(
    string Reason,
    DateTimeOffset OccurredAt,
    string? SourceType = null,
    string? SourceId = null);

/// <summary>
/// 공정 hold 해제에 필요한 입력값 묶음입니다.
/// </summary>
/// <param name="Note">해제 메모입니다.</param>
/// <param name="OccurredAt">해제 시각입니다.</param>
/// <param name="ExpectedSourceType">검증할 hold 출처 유형입니다.</param>
/// <param name="ExpectedSourceId">검증할 hold 출처 식별자입니다.</param>
public sealed record OperationHoldReleaseRequest(
    string Note,
    DateTimeOffset OccurredAt,
    string? ExpectedSourceType = null,
    string? ExpectedSourceId = null);
