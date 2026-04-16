using Mes.Application.OperatorExecution.WorkQueue;
using Mes.Domain.Aggregates;
using Mes.Domain.Common;
using Mes.Domain.Entities;
using Mes.Domain.Statuses;
using Mes.Domain.ValueObjects;

namespace Mes.Application.Tests;

/// <summary>
/// 작업 큐 read model source와 query 조합 규칙을 검증합니다.
/// </summary>
public sealed class StationWorkQueueReadServiceTests
{
    /// <summary>
    /// 작업 큐가 MES-side 자재 snapshot만 읽고 품질 hold를 WIP 조인으로 계산하는지 검증합니다.
    /// </summary>
    [Fact]
    public void BuildSnapshot_should_use_mes_side_requirement_snapshots_and_quality_join()
    {
        var occurredAt = new DateTimeOffset(2026, 4, 16, 13, 0, 0, TimeSpan.Zero);
        var stationId = new StationId("ST-21");
        var readService = new StationWorkQueueReadService();
        var projector = new OperationMaterialRequirementProjector();

        var firstOperation = CreateRunningOperation("PO-5001", "OP-5001", stationId, 10, occurredAt);
        var secondOperation = CreateRunningOperation("PO-5002", "OP-5002", stationId, 20, occurredAt);
        var wipUnit = CreateInProcessWip("WIP-5001", "ITEM-5001", firstOperation.Id);
        var qualityRecord = CreateHeldQualityRecord("QR-5001", wipUnit.Id, occurredAt.AddMinutes(1));
        var requirementSnapshots = projector.ProjectFromOperationAttachment(
            new ProjectOperationMaterialRequirementsRequest(
                firstOperation.Id,
                occurredAt,
                [
                    new OperationMaterialRequirementInput(2, "MAT-B", new MeasuredQuantity(2m, "EA"), "BOM-A"),
                    new OperationMaterialRequirementInput(1, "MAT-A", new MeasuredQuantity(1m, "EA"), "BOM-A")
                ]));

        var snapshot = readService.BuildSnapshot(
            new GetStationWorkQueueQueryRequest(
                stationId,
                occurredAt.AddMinutes(2),
                new StationWorkQueueSource(
                    [firstOperation, secondOperation],
                    [wipUnit],
                    [qualityRecord],
                    requirementSnapshots)));

        Assert.Equal(2, snapshot.Items.Count);

        var firstItem = snapshot.Items[0];
        Assert.Equal(firstOperation.Id, firstItem.Identity.OperationExecutionId);
        Assert.Equal(StationWorkQueueQualityGateState.Hold, firstItem.State.QualityGateState);
        Assert.Equal(["MAT-A", "MAT-B"], firstItem.RequiredMaterials.Select(material => material.MaterialCode).ToArray());

        var secondItem = snapshot.Items[1];
        Assert.Equal(secondOperation.Id, secondItem.Identity.OperationExecutionId);
        Assert.Equal(StationWorkQueueQualityGateState.Open, secondItem.State.QualityGateState);
        Assert.Empty(secondItem.RequiredMaterials);
    }

    /// <summary>
    /// 작업 큐가 스테이션 기준으로 필터링되고 operation 자체 hold도 gate hold로 해석하는지 검증합니다.
    /// </summary>
    [Fact]
    public void BuildSnapshot_should_filter_by_station_and_use_operation_hold_for_gate_state()
    {
        var occurredAt = new DateTimeOffset(2026, 4, 16, 13, 10, 0, TimeSpan.Zero);
        var targetStation = new StationId("ST-22");
        var otherStation = new StationId("ST-23");
        var readService = new StationWorkQueueReadService();

        var heldOperation = CreateRunningOperation("PO-5003", "OP-5003", targetStation, 10, occurredAt);
        heldOperation.PlaceHold(new OperationHoldRequest("maintenance lock", occurredAt.AddMinutes(1), HoldSourceTypes.Manual, "MAINT-22"));

        var otherOperation = CreateRunningOperation("PO-5004", "OP-5004", otherStation, 10, occurredAt);

        var snapshot = readService.BuildSnapshot(
            new GetStationWorkQueueQueryRequest(
                targetStation,
                occurredAt.AddMinutes(2),
                new StationWorkQueueSource(
                    [heldOperation, otherOperation],
                    [],
                    [],
                    [])));

        Assert.Single(snapshot.Items);
        Assert.Equal(heldOperation.Id, snapshot.Items[0].Identity.OperationExecutionId);
        Assert.Equal(StationWorkQueueQualityGateState.Hold, snapshot.Items[0].State.QualityGateState);
    }

    /// <summary>
    /// operation-attachment projector가 deterministic 식별자와 sequence 정렬을 유지하는지 검증합니다.
    /// </summary>
    [Fact]
    public void ProjectFromOperationAttachment_should_create_sorted_deterministic_snapshots()
    {
        var occurredAt = new DateTimeOffset(2026, 4, 16, 13, 20, 0, TimeSpan.Zero);
        var projector = new OperationMaterialRequirementProjector();
        var operationId = new OperationExecutionId("OP-5005");

        var snapshots = projector.ProjectFromOperationAttachment(
            new ProjectOperationMaterialRequirementsRequest(
                operationId,
                occurredAt,
                [
                    new OperationMaterialRequirementInput(3, "MAT-C", new MeasuredQuantity(3m, "EA"), "BOM-C"),
                    new OperationMaterialRequirementInput(1, "MAT-A", new MeasuredQuantity(1m, "EA"), "BOM-C"),
                    new OperationMaterialRequirementInput(2, "MAT-B", new MeasuredQuantity(2m, "EA"), "BOM-C")
                ]));

        Assert.Equal(
            ["OP-5005-REQ-001", "OP-5005-REQ-002", "OP-5005-REQ-003"],
            snapshots.Select(snapshot => snapshot.OperationMaterialRequirementId).ToArray());
        Assert.Equal([1, 2, 3], snapshots.Select(snapshot => snapshot.Metadata.SequenceNo).ToArray());
    }

    /// <summary>
    /// 같은 공정 snapshot 안에서 duplicate sequence가 거부되는지 검증합니다.
    /// </summary>
    [Fact]
    public void ProjectFromOperationAttachment_should_reject_duplicate_sequence()
    {
        var occurredAt = new DateTimeOffset(2026, 4, 16, 13, 30, 0, TimeSpan.Zero);
        var projector = new OperationMaterialRequirementProjector();

        Assert.Throws<InvalidOperationException>(() => projector.ProjectFromOperationAttachment(
            new ProjectOperationMaterialRequirementsRequest(
                new OperationExecutionId("OP-5006"),
                occurredAt,
                [
                    new OperationMaterialRequirementInput(1, "MAT-A", new MeasuredQuantity(1m, "EA"), "BOM-D"),
                    new OperationMaterialRequirementInput(1, "MAT-B", new MeasuredQuantity(2m, "EA"), "BOM-D")
                ])));
    }

    /// <summary>
    /// 실행 중인 공정 실행 aggregate를 테스트용으로 준비합니다.
    /// </summary>
    /// <param name="productionOrderId">상위 생산 오더 식별자입니다.</param>
    /// <param name="operationExecutionId">공정 실행 식별자입니다.</param>
    /// <param name="stationId">배정 스테이션 식별자입니다.</param>
    /// <param name="operationSequence">공정 순번입니다.</param>
    /// <param name="occurredAt">시작 시각입니다.</param>
    /// <returns>실행 중 상태의 공정 실행 aggregate입니다.</returns>
    private static OperationExecution CreateRunningOperation(
        string productionOrderId,
        string operationExecutionId,
        StationId stationId,
        int operationSequence,
        DateTimeOffset occurredAt)
    {
        var operation = OperationExecution.Create(
            new OperationExecutionId(operationExecutionId),
            new ProductionOrderId(productionOrderId),
            operationSequence);

        operation.QueueForExecution();
        operation.Start(stationId, occurredAt);
        return operation;
    }

    /// <summary>
    /// 실행 중인 WIP를 테스트용으로 준비합니다.
    /// </summary>
    /// <param name="wipUnitId">WIP 식별자입니다.</param>
    /// <param name="productCode">품목 코드입니다.</param>
    /// <param name="operationExecutionId">현재 공정 실행 식별자입니다.</param>
    /// <returns>해당 공정 실행에 연결된 WIP입니다.</returns>
    private static WipUnit CreateInProcessWip(
        string wipUnitId,
        string productCode,
        OperationExecutionId operationExecutionId)
    {
        var wipUnit = new WipUnit(new WipUnitId(wipUnitId), productCode);
        wipUnit.StartProcessing(operationExecutionId);
        return wipUnit;
    }

    /// <summary>
    /// hold 상태의 품질 기록 aggregate를 테스트용으로 준비합니다.
    /// </summary>
    /// <param name="qualityRecordId">품질 기록 식별자입니다.</param>
    /// <param name="wipUnitId">연결 WIP 식별자입니다.</param>
    /// <param name="occurredAt">품질 판정 시각입니다.</param>
    /// <returns>hold 상태의 품질 기록 aggregate입니다.</returns>
    private static QualityRecord CreateHeldQualityRecord(
        string qualityRecordId,
        WipUnitId wipUnitId,
        DateTimeOffset occurredAt)
    {
        var qualityRecord = QualityRecord.Create(
            new QualityRecordId(qualityRecordId),
            wipUnitId,
            "INSP-Q");

        qualityRecord.BeginInspection();
        qualityRecord.RecordFail("dimension mismatch", occurredAt);
        qualityRecord.PlaceHold("quality gate active", occurredAt);
        return qualityRecord;
    }
}
