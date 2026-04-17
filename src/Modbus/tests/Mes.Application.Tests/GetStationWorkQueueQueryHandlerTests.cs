using Mes.Application.Contracts.OperatorExecution;
using Mes.Application.OperatorExecution;
using Mes.Application.OperatorExecution.WorkQueue;
using Mes.Domain.Aggregates;
using Mes.Domain.Entities;
using Mes.Domain.ValueObjects;

namespace Mes.Application.Tests;

/// <summary>
/// 작업 큐 query handler의 contract 매핑 규칙을 검증합니다.
/// </summary>
public sealed class GetStationWorkQueueQueryHandlerTests
{
    /// <summary>
    /// query handler가 MES-side snapshot을 계약 응답으로 변환하는지 검증합니다.
    /// </summary>
    [Fact]
    public void Handle_should_map_snapshot_to_contract_response()
    {
        var occurredAt = new DateTimeOffset(2026, 4, 16, 17, 0, 0, TimeSpan.Zero);
        var stationId = new StationId("ST-81");
        var readService = new StationWorkQueueReadService();
        var queryHandler = new GetStationWorkQueueQueryHandler(readService);
        var projector = new OperationMaterialRequirementProjector();

        var operation = CreateRunningOperation("PO-8001", "OP-8001", stationId, occurredAt.AddMinutes(-10));
        var wipUnit = new WipUnit(new WipUnitId("WIP-8001"), "ITEM-8001");
        wipUnit.StartProcessing(operation.Id);
        var qualityRecord = QualityRecord.Create(new QualityRecordId("QR-8001"), wipUnit.Id, "INSP-81");
        qualityRecord.BeginInspection();
        qualityRecord.RecordFail("dimension mismatch", occurredAt.AddMinutes(-5));
        qualityRecord.PlaceHold("quality gate active", occurredAt.AddMinutes(-4));

        var requirements = projector.ProjectFromOperationAttachment(
            new ProjectOperationMaterialRequirementsRequest(
                operation.Id,
                occurredAt.AddMinutes(-8),
                [
                    new OperationMaterialRequirementInput(1, "MAT-81-A", new MeasuredQuantity(1m, "EA"), "BOM-81"),
                    new OperationMaterialRequirementInput(2, "MAT-81-B", new MeasuredQuantity(2m, "EA"), "BOM-81")
                ]));

        var response = queryHandler.Handle(
            new GetStationWorkQueueHandlingRequest(
                new GetStationWorkQueueRequestContract(stationId.ToString()),
                new StationWorkQueueSource([operation], [wipUnit], [qualityRecord], requirements),
                occurredAt));

        Assert.Equal(stationId.ToString(), response.StationId);
        Assert.Single(response.Items);
        Assert.Equal(operation.Id.ToString(), response.Items[0].OperationExecutionId);
        Assert.Equal(QualityGateStateValues.Hold, response.Items[0].QualityGateState);
        Assert.Equal("KG", response.Items[0].OperationQuantityUnit);
        Assert.Equal(["MAT-81-A", "MAT-81-B"], response.Items[0].RequiredMaterials.Select(material => material.MaterialCode).ToArray());
        Assert.Equal(1m, response.Items[0].RequiredMaterials[0].RequiredQuantity.Value);
        Assert.Equal(2m, response.Items[0].RequiredMaterials[1].RequiredQuantity.Value);
    }

    /// <summary>
    /// 테스트용 running 공정 실행 aggregate를 생성합니다.
    /// </summary>
    /// <param name="productionOrderId">생산오더 식별자입니다.</param>
    /// <param name="operationExecutionId">공정 실행 식별자입니다.</param>
    /// <param name="stationId">작업 스테이션 식별자입니다.</param>
    /// <param name="startedAt">시작 시각입니다.</param>
    /// <returns>running 상태의 공정 실행 aggregate입니다.</returns>
    private static OperationExecution CreateRunningOperation(
        string productionOrderId,
        string operationExecutionId,
        StationId stationId,
        DateTimeOffset startedAt)
    {
        var operation = OperationExecution.Create(
            new OperationExecutionId(operationExecutionId),
            new ProductionOrderId(productionOrderId),
            10,
            "KG");

        operation.QueueForExecution();
        operation.Start(stationId, startedAt);
        return operation;
    }
}
