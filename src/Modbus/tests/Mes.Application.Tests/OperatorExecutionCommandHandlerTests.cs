using Mes.Application.Contracts.Common;
using Mes.Application.Contracts.OperatorExecution;
using Mes.Application.Idempotency;
using Mes.Application.OperatorExecution;
using Mes.Domain.Aggregates;
using Mes.Domain.Entities;
using Mes.Domain.Statuses;
using Mes.Domain.ValueObjects;

namespace Mes.Application.Tests;

/// <summary>
/// 운영자 실행 command handler의 수락, replay, actuals 준비 규칙을 검증합니다.
/// </summary>
public sealed class OperatorExecutionCommandHandlerTests
{
    /// <summary>
    /// 공정 시작 명령이 aggregate 상태를 갱신하고 receipt를 남기는지 검증합니다.
    /// </summary>
    [Fact]
    public void Handle_start_operation_should_start_operation_and_store_receipt()
    {
        var serverReceivedAt = new DateTimeOffset(2026, 4, 16, 16, 20, 0, TimeSpan.Zero);
        var handler = CreateHandler();
        var order = CreateProductionOrder("PO-7001");
        var operation = CreateQueuedOperation(order.Id, "OP-7001", 10);
        order.AttachOperation(operation.Id);

        var command = new StartOperationCommandContract(
            CreateContext("CMD-7001", "CORR-7001", "KEY-7001", "operator-71", "ST-71"),
            new StartOperationPayloadContract(order.Id.ToString(), operation.Id.ToString(), 10, "EA"));

        var result = handler.Handle(
            new OperatorExecutionCommandHandlingRequest<StartOperationCommandContract, StartOperationCommandState>(
                command,
                new StartOperationCommandState(order, operation),
                null,
                serverReceivedAt));

        Assert.Equal(CommandReceiptDecisionKind.AcceptNew, result.Decision);
        Assert.True(result.Response.Accepted);
        Assert.Equal(OperationExecutionStatus.Running.ToString(), result.Response.Status);
        Assert.Equal(ProductionOrderStatus.InProgress, order.Status);
        Assert.NotNull(result.ReceiptToStore);
        Assert.Equal(command.CommandId, result.ReceiptToStore!.CommandId);
        Assert.Equal(OperatorExecutionCommandTypes.StartOperation, result.ReceiptToStore.Scope.CommandType);
        Assert.Equal(operation.Id.ToString(), result.ReceiptToStore.AggregateId);
    }

    /// <summary>
    /// 자재 소모 명령이 line issue와 genealogy 생성까지 함께 수행하는지 검증합니다.
    /// </summary>
    [Fact]
    public void Handle_material_consumption_should_issue_lot_and_create_genealogy()
    {
        var serverReceivedAt = new DateTimeOffset(2026, 4, 16, 16, 30, 0, TimeSpan.Zero);
        var handler = CreateHandler();
        var operation = CreateRunningOperation("PO-7002", "OP-7002", "ST-72", 10, serverReceivedAt.AddMinutes(-5));
        var wipUnit = new WipUnit(new WipUnitId("WIP-7002"), "ITEM-7002");
        wipUnit.StartProcessing(operation.Id);
        var materialLot = MaterialLot.Create(new MaterialLotId("LOT-7002"), "MAT-7002", new MeasuredQuantity(5m, "EA"));

        var command = new RecordMaterialConsumptionCommandContract(
            CreateContext("CMD-7002", "CORR-7002", "KEY-7002", "operator-72", "ST-72"),
            new RecordMaterialConsumptionPayloadContract(
                operation.Id.ToString(),
                wipUnit.Id.ToString(),
                materialLot.Id.ToString(),
                materialLot.MaterialCode,
                new MeasuredQuantityContract(2m, "EA")));

        var result = handler.Handle(
            new OperatorExecutionCommandHandlingRequest<RecordMaterialConsumptionCommandContract, RecordMaterialConsumptionCommandState>(
                command,
                new RecordMaterialConsumptionCommandState(operation, wipUnit, materialLot),
                null,
                serverReceivedAt));

        Assert.Equal(CommandReceiptDecisionKind.AcceptNew, result.Decision);
        Assert.Equal(MaterialLotStatus.Issued, materialLot.Status);
        Assert.Equal(3m, result.Response.RemainingQuantity.Value);
        Assert.True(result.Response.GenealogyLinkCreated);
        Assert.Single(materialLot.GenealogyLinks);
    }

    /// <summary>
    /// 동일한 품질 결과 명령이 같은 receipt로 replay되는지 검증합니다.
    /// </summary>
    [Fact]
    public void Handle_quality_result_should_replay_stored_response_on_safe_retry()
    {
        var firstReceivedAt = new DateTimeOffset(2026, 4, 16, 16, 40, 0, TimeSpan.Zero);
        var handler = CreateHandler();
        var operation = CreateRunningOperation("PO-7003", "OP-7003", "ST-73", 10, firstReceivedAt.AddMinutes(-5));
        var wipUnit = new WipUnit(new WipUnitId("WIP-7003"), "ITEM-7003");
        wipUnit.StartProcessing(operation.Id);
        var qualityRecord = QualityRecord.Create(new QualityRecordId("QR-7003"), wipUnit.Id, "INSP-73");
        qualityRecord.BeginInspection();

        var command = new RecordQualityResultCommandContract(
            CreateContext("CMD-7003", "CORR-7003", "KEY-7003", "operator-73", "ST-73"),
            new RecordQualityResultPayloadContract(
                qualityRecord.Id.ToString(),
                wipUnit.Id.ToString(),
                qualityRecord.InspectionCode,
                QualityDecisionValues.Failed,
                "dimension mismatch"));

        var firstResult = handler.Handle(
            new OperatorExecutionCommandHandlingRequest<RecordQualityResultCommandContract, RecordQualityResultCommandState>(
                command,
                new RecordQualityResultCommandState(qualityRecord, wipUnit, operation),
                null,
                firstReceivedAt));

        var qualityEventCount = qualityRecord.DomainEvents.Count;
        var operationEventCount = operation.DomainEvents.Count;

        var replayResult = handler.Handle(
            new OperatorExecutionCommandHandlingRequest<RecordQualityResultCommandContract, RecordQualityResultCommandState>(
                command,
                new RecordQualityResultCommandState(qualityRecord, wipUnit, operation),
                firstResult.ReceiptToStore,
                firstReceivedAt.AddMinutes(1)));

        Assert.Equal(CommandReceiptDecisionKind.ReplayStored, replayResult.Decision);
        Assert.Equal(firstResult.Response, replayResult.Response);
        Assert.Null(replayResult.ReceiptToStore);
        Assert.Equal(qualityEventCount, qualityRecord.DomainEvents.Count);
        Assert.Equal(operationEventCount, operation.DomainEvents.Count);
    }

    /// <summary>
    /// 공정 완료 명령이 pending-projection production actuals batch를 준비하는지 검증합니다.
    /// </summary>
    [Fact]
    public void Handle_complete_operation_should_prepare_pending_actuals_batch()
    {
        var serverReceivedAt = new DateTimeOffset(2026, 4, 16, 16, 50, 0, TimeSpan.Zero);
        var handler = CreateHandler();
        var order = CreateProductionOrder("PO-7004");
        var operation = CreateRunningOperation(order.Id.ToString(), "OP-7004", "ST-74", 10, serverReceivedAt.AddMinutes(-10));
        order.AttachOperation(operation.Id);

        var command = new CompleteOperationCommandContract(
            CreateContext("CMD-7004", "CORR-7004", "KEY-7004", "operator-74", "ST-74"),
            new CompleteOperationPayloadContract(
                operation.Id.ToString(),
                new MeasuredQuantityContract(8m, "EA"),
                new MeasuredQuantityContract(2m, "EA"),
                CompletionModeValues.Manual));

        var result = handler.Handle(
            new OperatorExecutionCommandHandlingRequest<CompleteOperationCommandContract, CompleteOperationCommandState>(
                command,
                new CompleteOperationCommandState(
                    order,
                    operation,
                    new OrderCompletionProgressSnapshot(order.Id.ToString(), 1, 0, 1)),
                null,
                serverReceivedAt));

        Assert.Equal(CommandReceiptDecisionKind.AcceptNew, result.Decision);
        Assert.Equal(OperationExecutionStatus.Done, operation.Status);
        Assert.Equal(8m, operation.GoodQuantity.Value);
        Assert.Equal(2m, operation.ScrapQuantity.Value);
        Assert.Equal(ProductionOrderStatus.Completed, order.Status);
        Assert.Equal(ProductionActualsStatusValues.PendingProjection, result.PreparedBatch.Status);
        Assert.Equal($"ACT-{order.Id}-{operation.Id}", result.PreparedBatch.ActualsBatchId);
        Assert.NotNull(result.ReceiptToStore);
    }

    /// <summary>
    /// 현재 공정 외에 미완료 sibling operation이 남아 있으면 생산오더를 부분완료로 올리는지 검증합니다.
    /// </summary>
    [Fact]
    public void Handle_complete_operation_should_mark_order_partially_completed_when_siblings_remain_open()
    {
        var serverReceivedAt = new DateTimeOffset(2026, 4, 16, 16, 55, 0, TimeSpan.Zero);
        var handler = CreateHandler();
        var order = CreateProductionOrder("PO-7005");
        var currentOperation = CreateRunningOperation(order.Id.ToString(), "OP-7005", "ST-75", 10, serverReceivedAt.AddMinutes(-10));
        var remainingSiblingOperation = CreateQueuedOperation(order.Id, "OP-7006", 20);
        order.AttachOperation(currentOperation.Id);
        order.AttachOperation(remainingSiblingOperation.Id);
        order.MarkInProgress();

        var command = new CompleteOperationCommandContract(
            CreateContext("CMD-7005", "CORR-7005", "KEY-7005", "operator-75", "ST-75"),
            new CompleteOperationPayloadContract(
                currentOperation.Id.ToString(),
                new MeasuredQuantityContract(4m, "EA"),
                new MeasuredQuantityContract(0m, "EA"),
                CompletionModeValues.Manual));

        var result = handler.Handle(
            new OperatorExecutionCommandHandlingRequest<CompleteOperationCommandContract, CompleteOperationCommandState>(
                command,
                new CompleteOperationCommandState(
                    order,
                    currentOperation,
                    new OrderCompletionProgressSnapshot(order.Id.ToString(), 2, 1, 1)),
                null,
                serverReceivedAt));

        Assert.Equal(CommandReceiptDecisionKind.AcceptNew, result.Decision);
        Assert.Equal(OperationExecutionStatus.Done, currentOperation.Status);
        Assert.Equal(ProductionOrderStatus.PartiallyCompleted, order.Status);
        Assert.Equal(ProductionActualsStatusValues.PendingProjection, result.PreparedBatch.Status);
    }

    /// <summary>
    /// 테스트용 handler 인스턴스를 생성합니다.
    /// </summary>
    /// <returns>기본 의존성으로 구성한 handler입니다.</returns>
    private static OperatorExecutionCommandHandler CreateHandler()
    {
        return new OperatorExecutionCommandHandler(
            new CanonicalCommandFingerprintBuilder(),
            new CommandReceiptIdempotencyPolicy(),
            new StoredCommandResponseSerializer(),
            new QualityHoldGateCoordinator(),
            new ProductionActualsPreparationService());
    }

    /// <summary>
    /// 테스트용 command context를 생성합니다.
    /// </summary>
    /// <param name="commandId">명령 식별자입니다.</param>
    /// <param name="correlationId">상관관계 식별자입니다.</param>
    /// <param name="idempotencyKey">idempotency 키입니다.</param>
    /// <param name="actorId">행위자 식별자입니다.</param>
    /// <param name="stationId">스테이션 식별자입니다.</param>
    /// <returns>command context 계약입니다.</returns>
    private static CommandContextContract CreateContext(
        string commandId,
        string correlationId,
        string idempotencyKey,
        string actorId,
        string stationId)
    {
        return new CommandContextContract(
            new CommandIdentityContract(commandId, correlationId, idempotencyKey),
            new CommandOriginContract(actorId, BffChannelValues.Wpf, stationId),
            new DateTimeOffset(2026, 4, 16, 16, 0, 0, TimeSpan.Zero),
            null);
    }

    /// <summary>
    /// 테스트용 생산오더를 생성합니다.
    /// </summary>
    /// <param name="productionOrderId">생산오더 식별자입니다.</param>
    /// <returns>released 상태의 생산오더입니다.</returns>
    private static ProductionOrder CreateProductionOrder(string productionOrderId)
    {
        return ProductionOrder.Release(
            new ProductionOrderId(productionOrderId),
            "ITEM-" + productionOrderId,
            "ROUTE-A",
            new DateTimeOffset(2026, 4, 16, 15, 0, 0, TimeSpan.Zero));
    }

    /// <summary>
    /// 테스트용 queued 공정 실행 aggregate를 생성합니다.
    /// </summary>
    /// <param name="productionOrderId">상위 생산오더 식별자입니다.</param>
    /// <param name="operationExecutionId">공정 실행 식별자입니다.</param>
    /// <param name="operationSequence">공정 순번입니다.</param>
    /// <returns>queued 상태의 공정 실행 aggregate입니다.</returns>
    private static OperationExecution CreateQueuedOperation(
        ProductionOrderId productionOrderId,
        string operationExecutionId,
        int operationSequence)
    {
        var operation = OperationExecution.Create(
            new OperationExecutionId(operationExecutionId),
            productionOrderId,
            operationSequence);

        operation.QueueForExecution();
        return operation;
    }

    /// <summary>
    /// 테스트용 running 공정 실행 aggregate를 생성합니다.
    /// </summary>
    /// <param name="productionOrderId">상위 생산오더 식별자입니다.</param>
    /// <param name="operationExecutionId">공정 실행 식별자입니다.</param>
    /// <param name="stationId">작업 스테이션 식별자입니다.</param>
    /// <param name="operationSequence">공정 순번입니다.</param>
    /// <param name="startedAt">시작 시각입니다.</param>
    /// <returns>running 상태의 공정 실행 aggregate입니다.</returns>
    private static OperationExecution CreateRunningOperation(
        string productionOrderId,
        string operationExecutionId,
        string stationId,
        int operationSequence,
        DateTimeOffset startedAt)
    {
        var operation = OperationExecution.Create(
            new OperationExecutionId(operationExecutionId),
            new ProductionOrderId(productionOrderId),
            operationSequence);

        operation.QueueForExecution();
        operation.Start(new StationId(stationId), startedAt);
        return operation;
    }
}
