using Mes.Application.OperatorExecution;
using Mes.Domain.Abstractions;
using Mes.Domain.Aggregates;
using Mes.Domain.Common;
using Mes.Domain.Statuses;
using Mes.Domain.ValueObjects;

namespace Mes.Application.Tests;

/// <summary>
/// release-1 품질 hold coordinator의 교차 aggregate 규칙을 검증합니다.
/// </summary>
public sealed class QualityHoldGateCoordinatorTests
{
    /// <summary>
    /// blocking 품질 판정이 품질 기록과 공정 실행 hold를 함께 만들고, 해제 시 이전 실행 상태를 복원하는지 검증합니다.
    /// </summary>
    [Fact]
    public void Blocking_quality_result_should_materialize_and_restore_quality_gate_state()
    {
        var occurredAt = new DateTimeOffset(2026, 4, 16, 12, 0, 0, TimeSpan.Zero);
        var operation = CreateRunningOperation("PO-4001", "OP-4001", "ST-11", occurredAt);
        var qualityRecord = CreateInspectingQualityRecord("QR-4001", "WIP-4001", "INSP-11");
        var coordinator = new QualityHoldGateCoordinator();
        var request = new RecordQualityDecisionRequest(
            new QualityGateContext(qualityRecord, operation),
            QualityDecisionStatus.Failed,
            new QualityDecisionPolicy(true, "dimension out of spec", "quality gate active"),
            occurredAt.AddMinutes(1));

        var heldSnapshot = coordinator.RecordQualityResult(request);

        Assert.Equal(QualityRecordStatus.Hold, qualityRecord.Status);
        Assert.Equal(QualityDecisionStatus.Failed, qualityRecord.DecisionStatus);
        Assert.Equal(OperationExecutionStatus.Hold, operation.Status);
        Assert.Equal(OperationExecutionStatus.Running, operation.StatusBeforeHold);
        Assert.Equal(HoldSourceTypes.QualityRecord, operation.HoldSourceType);
        Assert.Equal(qualityRecord.Id.ToString(), operation.HoldSourceId);
        Assert.False(heldSnapshot.QualityGateOpen);
        Assert.Throws<DomainException>(() => coordinator.EnsureCompletionAllowed(operation));

        var releasedSnapshot = coordinator.ReleaseQualityHold(new ReleaseQualityHoldRequest(
            new QualityGateContext(qualityRecord, operation),
            "recheck accepted",
            occurredAt.AddMinutes(2)));

        Assert.Equal(QualityRecordStatus.Released, qualityRecord.Status);
        Assert.Equal(QualityDecisionStatus.Failed, qualityRecord.DecisionStatus);
        Assert.Equal(OperationExecutionStatus.Running, operation.Status);
        Assert.Null(operation.StatusBeforeHold);
        Assert.Null(operation.HoldSourceType);
        Assert.Null(operation.HoldSourceId);
        Assert.True(releasedSnapshot.QualityGateOpen);
        coordinator.EnsureCompletionAllowed(operation);
    }

    /// <summary>
    /// 동일한 blocking 품질 판정 재실행이 추가 mutation 없이 안전하게 재생되는지 검증합니다.
    /// </summary>
    [Fact]
    public void Duplicate_blocking_quality_result_should_replay_without_duplicate_domain_events()
    {
        var occurredAt = new DateTimeOffset(2026, 4, 16, 12, 10, 0, TimeSpan.Zero);
        var operation = CreateRunningOperation("PO-4002", "OP-4002", "ST-12", occurredAt);
        var qualityRecord = CreateInspectingQualityRecord("QR-4002", "WIP-4002", "INSP-12");
        var coordinator = new QualityHoldGateCoordinator();
        var request = new RecordQualityDecisionRequest(
            new QualityGateContext(qualityRecord, operation),
            QualityDecisionStatus.Failed,
            new QualityDecisionPolicy(true, "scratch detected", "quality gate active"),
            occurredAt.AddMinutes(1));

        coordinator.RecordQualityResult(request);
        var qualityEventCount = qualityRecord.DomainEvents.Count;
        var operationEventCount = operation.DomainEvents.Count;

        var replaySnapshot = coordinator.RecordQualityResult(request);

        Assert.Equal(qualityEventCount, qualityRecord.DomainEvents.Count);
        Assert.Equal(operationEventCount, operation.DomainEvents.Count);
        Assert.Equal(QualityRecordStatus.Hold, replaySnapshot.QualityStatus);
        Assert.Equal(OperationExecutionStatus.Hold, replaySnapshot.OperationStatus);
        Assert.False(replaySnapshot.QualityGateOpen);
    }

    /// <summary>
    /// 품질 기록만 hold 상태이고 공정 실행이 아직 활성 상태이면 coordinator가 operation hold를 보정하는지 검증합니다.
    /// </summary>
    [Fact]
    public void Reconcile_should_apply_operation_hold_when_quality_is_held_but_operation_is_not()
    {
        var occurredAt = new DateTimeOffset(2026, 4, 16, 12, 20, 0, TimeSpan.Zero);
        var operation = CreateRunningOperation("PO-4003", "OP-4003", "ST-13", occurredAt);
        var qualityRecord = CreateInspectingQualityRecord("QR-4003", "WIP-4003", "INSP-13");
        var coordinator = new QualityHoldGateCoordinator();

        qualityRecord.RecordFail("vision inspection failed", occurredAt.AddMinutes(1));
        qualityRecord.PlaceHold("quality gate active", occurredAt.AddMinutes(1));

        var snapshot = coordinator.Reconcile(new QualityGateReconciliationRequest(
            new QualityGateContext(qualityRecord, operation),
            occurredAt.AddMinutes(2)));

        Assert.Equal(QualityRecordStatus.Hold, qualityRecord.Status);
        Assert.Equal(OperationExecutionStatus.Hold, operation.Status);
        Assert.Equal(OperationExecutionStatus.Running, operation.StatusBeforeHold);
        Assert.Equal(HoldSourceTypes.QualityRecord, operation.HoldSourceType);
        Assert.Equal(qualityRecord.Id.ToString(), operation.HoldSourceId);
        Assert.False(snapshot.QualityGateOpen);
    }

    /// <summary>
    /// 품질 hold를 해제해도 다른 출처가 소유한 operation hold는 유지되는지 검증합니다.
    /// </summary>
    [Fact]
    public void Release_should_not_clear_operation_hold_owned_by_another_source()
    {
        var occurredAt = new DateTimeOffset(2026, 4, 16, 12, 30, 0, TimeSpan.Zero);
        var operation = CreateRunningOperation("PO-4004", "OP-4004", "ST-14", occurredAt);
        var qualityRecord = CreateInspectingQualityRecord("QR-4004", "WIP-4004", "INSP-14");
        var coordinator = new QualityHoldGateCoordinator();

        qualityRecord.RecordFail("dimension mismatch", occurredAt.AddMinutes(1));
        qualityRecord.PlaceHold("quality gate active", occurredAt.AddMinutes(1));
        operation.PlaceHold(new OperationHoldRequest(
            "maintenance lock",
            occurredAt.AddMinutes(1),
            HoldSourceTypes.Manual,
            "MAINT-01"));

        var snapshot = coordinator.ReleaseQualityHold(new ReleaseQualityHoldRequest(
            new QualityGateContext(qualityRecord, operation),
            "quality recheck accepted",
            occurredAt.AddMinutes(2)));

        Assert.Equal(QualityRecordStatus.Released, qualityRecord.Status);
        Assert.Equal(OperationExecutionStatus.Hold, operation.Status);
        Assert.Equal(HoldSourceTypes.Manual, operation.HoldSourceType);
        Assert.Equal("MAINT-01", operation.HoldSourceId);
        Assert.Equal(OperationExecutionStatus.Running, operation.StatusBeforeHold);
        Assert.False(snapshot.QualityGateOpen);
    }

    /// <summary>
    /// 실행 중인 공정 aggregate를 테스트용으로 준비합니다.
    /// </summary>
    /// <param name="productionOrderId">상위 생산 오더 식별자입니다.</param>
    /// <param name="operationExecutionId">공정 실행 식별자입니다.</param>
    /// <param name="stationId">작업 스테이션 식별자입니다.</param>
    /// <param name="occurredAt">시작 시각입니다.</param>
    /// <returns>실행 중 상태의 공정 실행 aggregate입니다.</returns>
    private static OperationExecution CreateRunningOperation(
        string productionOrderId,
        string operationExecutionId,
        string stationId,
        DateTimeOffset occurredAt)
    {
        var operation = OperationExecution.Create(
            new OperationExecutionId(operationExecutionId),
            new ProductionOrderId(productionOrderId),
            10);

        operation.QueueForExecution();
        operation.Start(new StationId(stationId), occurredAt);
        return operation;
    }

    /// <summary>
    /// 검사 진행 중 상태의 품질 기록 aggregate를 테스트용으로 준비합니다.
    /// </summary>
    /// <param name="qualityRecordId">품질 기록 식별자입니다.</param>
    /// <param name="wipUnitId">대상 WIP 식별자입니다.</param>
    /// <param name="inspectionCode">검사 항목 코드입니다.</param>
    /// <returns>검사 진행 중 상태의 품질 기록 aggregate입니다.</returns>
    private static QualityRecord CreateInspectingQualityRecord(
        string qualityRecordId,
        string wipUnitId,
        string inspectionCode)
    {
        var qualityRecord = QualityRecord.Create(
            new QualityRecordId(qualityRecordId),
            new WipUnitId(wipUnitId),
            inspectionCode);

        qualityRecord.BeginInspection();
        return qualityRecord;
    }
}
