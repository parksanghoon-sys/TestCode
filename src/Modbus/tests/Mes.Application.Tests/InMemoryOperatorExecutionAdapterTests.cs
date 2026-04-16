using Mes.Application.Contracts.Common;
using Mes.Application.Contracts.OperatorExecution;
using Mes.Application.Idempotency;
using Mes.Application.OperatorExecution;
using Mes.Application.OperatorExecution.WorkQueue;
using Mes.Domain.Aggregates;
using Mes.Domain.Entities;
using Mes.Domain.Events;
using Mes.Domain.ValueObjects;
using Mes.Infrastructure.OperatorExecution;
using Mes.Infrastructure.OperatorExecution.InMemory;

namespace Mes.Application.Tests;

/// <summary>
/// in-memory 기준 adapter와 endpoint façade의 end-to-end 연결을 검증합니다.
/// </summary>
public sealed class InMemoryOperatorExecutionAdapterTests
{
    /// <summary>
    /// 공정 시작 endpoint가 receipt와 outbox를 함께 커밋하는지 검증합니다.
    /// </summary>
    [Fact]
    public async Task StartOperationAsync_should_commit_receipt_and_outbox()
    {
        var now = new DateTimeOffset(2026, 4, 16, 18, 0, 0, TimeSpan.Zero);
        var endpoint = CreateEndpoint(now, out var store);
        var order = CreateProductionOrder("PO-10001");
        var operation = CreateQueuedOperation(order.Id, "OP-10001", 10);
        order.AttachOperation(operation.Id);

        store.Seed(new InMemoryOperatorExecutionSeed
        {
            ProductionOrders = [order],
            OperationExecutions = [operation]
        });

        var response = await endpoint.StartOperationAsync(
            new StartOperationCommandContract(
                CreateContext("CMD-10001", "CORR-10001", "KEY-10001", "operator-101", "ST-101"),
                new StartOperationPayloadContract(order.Id.ToString(), operation.Id.ToString(), 10, "EA")));

        Assert.True(response.Accepted);
        Assert.Equal("Running", response.Status);
        Assert.Single(store.Receipts);
        Assert.Contains(store.OutboxEntries, entry => entry.DomainEvent is OperationStartedDomainEvent);
    }

    /// <summary>
    /// 동일 품질 결과 재시도는 receipt와 outbox를 중복 생성하지 않는지 검증합니다.
    /// </summary>
    [Fact]
    public async Task RecordQualityResultAsync_should_replay_without_duplicate_side_effects()
    {
        var now = new DateTimeOffset(2026, 4, 16, 18, 10, 0, TimeSpan.Zero);
        var endpoint = CreateEndpoint(now, out var store);
        var operation = CreateRunningOperation("PO-10002", "OP-10002", "ST-102", 10, now.AddMinutes(-5));
        var wipUnit = new WipUnit(new WipUnitId("WIP-10002"), "ITEM-10002");
        wipUnit.StartProcessing(operation.Id);
        var qualityRecord = QualityRecord.Create(new QualityRecordId("QR-10002"), wipUnit.Id, "INSP-102");
        qualityRecord.BeginInspection();

        store.Seed(new InMemoryOperatorExecutionSeed
        {
            OperationExecutions = [operation],
            WipUnits = [wipUnit],
            QualityRecords = [qualityRecord],
            ProductionOrders = [CreateProductionOrder("PO-10002")]
        });

        var command = new RecordQualityResultCommandContract(
            CreateContext("CMD-10002", "CORR-10002", "KEY-10002", "operator-102", "ST-102"),
            new RecordQualityResultPayloadContract(
                qualityRecord.Id.ToString(),
                wipUnit.Id.ToString(),
                qualityRecord.InspectionCode,
                QualityDecisionValues.Failed,
                "dimension mismatch"));

        var firstResponse = await endpoint.RecordQualityResultAsync(command);
        var receiptCountAfterFirst = store.Receipts.Count;
        var outboxCountAfterFirst = store.OutboxEntries.Count;

        var replayResponse = await endpoint.RecordQualityResultAsync(command);

        Assert.Equal(firstResponse, replayResponse);
        Assert.Equal(receiptCountAfterFirst, store.Receipts.Count);
        Assert.Equal(outboxCountAfterFirst, store.OutboxEntries.Count);
        Assert.Contains(store.OutboxEntries, entry => entry.DomainEvent is QualityResultRecordedDomainEvent);
        Assert.Contains(store.OutboxEntries, entry => entry.DomainEvent is HoldPlacedDomainEvent);
    }

    /// <summary>
    /// 공정 완료 endpoint가 actuals batch와 outbox를 함께 저장하는지 검증합니다.
    /// </summary>
    [Fact]
    public async Task CompleteOperationAsync_should_commit_batch_and_outbox()
    {
        var now = new DateTimeOffset(2026, 4, 16, 18, 20, 0, TimeSpan.Zero);
        var endpoint = CreateEndpoint(now, out var store);
        var order = CreateProductionOrder("PO-10003");
        var operation = CreateRunningOperation(order.Id.ToString(), "OP-10003", "ST-103", 10, now.AddMinutes(-15));
        order.AttachOperation(operation.Id);

        store.Seed(new InMemoryOperatorExecutionSeed
        {
            ProductionOrders = [order],
            OperationExecutions = [operation]
        });

        var response = await endpoint.CompleteOperationAsync(
            new CompleteOperationCommandContract(
                CreateContext("CMD-10003", "CORR-10003", "KEY-10003", "operator-103", "ST-103"),
                new CompleteOperationPayloadContract(
                    operation.Id.ToString(),
                    new MeasuredQuantityContract(6m, "EA"),
                    new MeasuredQuantityContract(1m, "EA"),
                    CompletionModeValues.Manual)));

        Assert.True(response.Accepted);
        Assert.Equal("Done", response.Status);
        Assert.Single(store.ProductionActualsBatches);
        Assert.Equal(ProductionActualsStatusValues.PendingProjection, store.ProductionActualsBatches[0].Status);
        Assert.Contains(store.OutboxEntries, entry => entry.DomainEvent is OperationCompletedDomainEvent);
    }

    /// <summary>
    /// 작업 큐 endpoint가 저장소 source와 요구 자재 snapshot을 그대로 반영하는지 검증합니다.
    /// </summary>
    [Fact]
    public async Task GetStationWorkQueueAsync_should_project_store_source()
    {
        var now = new DateTimeOffset(2026, 4, 16, 18, 30, 0, TimeSpan.Zero);
        var endpoint = CreateEndpoint(now, out var store);
        var stationId = new StationId("ST-104");
        var operation = CreateRunningOperation("PO-10004", "OP-10004", stationId.ToString(), 10, now.AddMinutes(-20));
        var projector = new OperationMaterialRequirementProjector();

        store.Seed(new InMemoryOperatorExecutionSeed
        {
            OperationExecutions = [operation],
            ProductionOrders = [CreateProductionOrder("PO-10004")],
            MaterialRequirements = projector.ProjectFromOperationAttachment(
                new ProjectOperationMaterialRequirementsRequest(
                    operation.Id,
                    now.AddMinutes(-10),
                    [new OperationMaterialRequirementInput(1, "MAT-104", new MeasuredQuantity(3m, "EA"), "BOM-104")]))
        });

        var response = await endpoint.GetStationWorkQueueAsync(
            new GetStationWorkQueueRequestContract(stationId.ToString()));

        Assert.Single(response.Items);
        Assert.Equal(operation.Id.ToString(), response.Items[0].OperationExecutionId);
        Assert.Equal("MAT-104", response.Items[0].RequiredMaterials[0].MaterialCode);
        Assert.Equal(QualityGateStateValues.Open, response.Items[0].QualityGateState);
    }

    /// <summary>
    /// 테스트용 endpoint façade를 생성합니다.
    /// </summary>
    /// <param name="now">고정 서버 시각입니다.</param>
    /// <param name="store">생성된 in-memory 저장소를 반환합니다.</param>
    /// <returns>구성된 endpoint façade입니다.</returns>
    private static OperatorExecutionBffEndpointAdapter CreateEndpoint(
        DateTimeOffset now,
        out InMemoryOperatorExecutionStore store)
    {
        store = new InMemoryOperatorExecutionStore();
        var commandPort = new InMemoryOperatorExecutionCommandAdapter(store, new CanonicalCommandFingerprintBuilder());
        var sourcePort = new InMemoryStationWorkQueueSourceAdapter(store);
        var applicationService = new OperatorExecutionApplicationService(
            new OperatorExecutionCommandHandler(
                new CanonicalCommandFingerprintBuilder(),
                new CommandReceiptIdempotencyPolicy(),
                new StoredCommandResponseSerializer(),
                new QualityHoldGateCoordinator(),
                new ProductionActualsPreparationService()),
            commandPort,
            new GetStationWorkQueueQueryHandler(new StationWorkQueueReadService()),
            sourcePort);

        return new OperatorExecutionBffEndpointAdapter(applicationService, new FixedTimeProvider(now));
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
            new DateTimeOffset(2026, 4, 16, 17, 55, 0, TimeSpan.Zero),
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
            new DateTimeOffset(2026, 4, 16, 17, 0, 0, TimeSpan.Zero));
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
    /// <param name="stationId">스테이션 식별자입니다.</param>
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

    /// <summary>
    /// 고정 시각을 반환하는 테스트용 time provider입니다.
    /// </summary>
    /// <param name="utcNow">반환할 현재 시각입니다.</param>
    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private readonly DateTimeOffset _utcNow = utcNow;

        /// <summary>
        /// 고정된 현재 UTC 시각을 반환합니다.
        /// </summary>
        /// <returns>고정된 UTC 시각입니다.</returns>
        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}
