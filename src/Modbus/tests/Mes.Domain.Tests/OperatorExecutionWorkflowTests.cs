using Mes.Domain.Abstractions;
using Mes.Domain.Aggregates;
using Mes.Domain.Entities;
using Mes.Domain.Events;
using Mes.Domain.Statuses;
using Mes.Domain.ValueObjects;

namespace Mes.Domain.Tests;

/// <summary>
/// 선택된 운영자 실행 파일럿 slice의 핵심 happy path와 hold gate 예외 경로를 검증합니다.
/// </summary>
public class OperatorExecutionWorkflowTests
{
    /// <summary>
    /// 작업 시작부터 자재 소모, 품질 합격, 작업 완료까지의 핵심 흐름이 현재 도메인 모델에서 일관되게 유지되는지 검증합니다.
    /// </summary>
    [Fact]
    public void Pilot_slice_happy_path_should_keep_order_execution_material_and_quality_in_sync()
    {
        var releasedAt = new DateTimeOffset(2026, 4, 16, 9, 0, 0, TimeSpan.Zero);
        var orderId = new ProductionOrderId("PO-3001");
        var operationId = new OperationExecutionId("OP-3001");
        var wipUnitId = new WipUnitId("WIP-3001");

        var order = ProductionOrder.Release(orderId, "ITEM-01", "ROUTE-A", releasedAt);
        order.AttachOperation(operationId);
        order.MarkInProgress();

        var operation = OperationExecution.Create(operationId, orderId, 10);
        operation.QueueForExecution();
        operation.Start(new StationId("ST-01"), releasedAt.AddMinutes(1));

        var wipUnit = new WipUnit(wipUnitId, "ITEM-01");
        wipUnit.QueueFor(operationId);
        wipUnit.StartProcessing(operationId);

        var materialLot = MaterialLot.Create(
            new MaterialLotId("LOT-3001"),
            "MAT-01",
            new MeasuredQuantity(3, "EA"));

        materialLot.IssueToLine();
        materialLot.ConsumeFor(wipUnitId, new MeasuredQuantity(1, "EA"), releasedAt.AddMinutes(2));

        var qualityRecord = QualityRecord.Create(
            new QualityRecordId("QR-3001"),
            wipUnitId,
            "INSP-01");

        qualityRecord.BeginInspection();
        qualityRecord.RecordPass("first piece accepted", releasedAt.AddMinutes(3));

        operation.Complete(new MeasuredQuantity(1, "EA"), releasedAt.AddMinutes(4));
        wipUnit.Complete();
        order.MarkPartiallyCompleted();

        Assert.Equal(ProductionOrderStatus.PartiallyCompleted, order.Status);
        Assert.Equal(OperationExecutionStatus.Done, operation.Status);
        Assert.Equal(WipUnitStatus.Completed, wipUnit.Status);
        Assert.Equal(2m, materialLot.AvailableQuantity.Value);
        Assert.Equal(1m, materialLot.ConsumedQuantity.Value);
        Assert.Equal(QualityRecordStatus.Passed, qualityRecord.Status);
        Assert.Contains(operation.DomainEvents, e => e is OperationStartedDomainEvent);
        Assert.Contains(operation.DomainEvents, e => e is OperationCompletedDomainEvent);
        Assert.Contains(materialLot.DomainEvents, e => e is MaterialConsumptionRecordedDomainEvent);
        Assert.Contains(materialLot.DomainEvents, e => e is GenealogyLinkCreatedDomainEvent);
        Assert.Contains(qualityRecord.DomainEvents, e => e is QualityResultRecordedDomainEvent);
    }

    /// <summary>
    /// 품질 hold gate가 걸린 작업은 release 이전에 완료될 수 없고, 해제 이후에만 완료로 진행되는지 검증합니다.
    /// </summary>
    [Fact]
    public void Pilot_slice_hold_gate_should_block_completion_until_both_records_are_released()
    {
        var occurredAt = new DateTimeOffset(2026, 4, 16, 10, 0, 0, TimeSpan.Zero);
        var operation = OperationExecution.Create(
            new OperationExecutionId("OP-3002"),
            new ProductionOrderId("PO-3002"),
            20);

        operation.QueueForExecution();
        operation.Start(new StationId("ST-02"), occurredAt);

        var qualityRecord = QualityRecord.Create(
            new QualityRecordId("QR-3002"),
            new WipUnitId("WIP-3002"),
            "INSP-02");

        qualityRecord.BeginInspection();
        qualityRecord.PlaceHold("dimension mismatch", occurredAt.AddMinutes(1));
        operation.PlaceHold("quality gate active", occurredAt.AddMinutes(1));

        Assert.Throws<DomainException>(() => operation.Complete(new MeasuredQuantity(1, "EA"), occurredAt.AddMinutes(2)));

        qualityRecord.ReleaseHold("recheck approved", occurredAt.AddMinutes(3));
        operation.ReleaseHold("quality gate cleared", occurredAt.AddMinutes(3));
        operation.Complete(new MeasuredQuantity(1, "EA"), occurredAt.AddMinutes(4));

        Assert.Equal(QualityRecordStatus.Released, qualityRecord.Status);
        Assert.Equal(OperationExecutionStatus.Done, operation.Status);
        Assert.Contains(qualityRecord.DomainEvents, e => e is HoldPlacedDomainEvent);
        Assert.Contains(qualityRecord.DomainEvents, e => e is HoldReleasedDomainEvent);
        Assert.Contains(operation.DomainEvents, e => e is HoldPlacedDomainEvent);
        Assert.Contains(operation.DomainEvents, e => e is HoldReleasedDomainEvent);
        Assert.Contains(operation.DomainEvents, e => e is OperationCompletedDomainEvent);
    }
}
