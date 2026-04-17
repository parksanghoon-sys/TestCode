using Mes.Application.Contracts.Common;
using Mes.Application.Contracts.OperatorExecution;
using Mes.Application.Idempotency;
using Mes.Application.OperatorExecution;
using Mes.Application.OperatorExecution.WorkQueue;
using Mes.Domain.Aggregates;
using Mes.Domain.Events;
using Mes.Domain.Statuses;
using Mes.Domain.ValueObjects;
using Mes.Infrastructure.OperatorExecution;
using Mes.Infrastructure.OperatorExecution.Sqlite;

namespace Mes.Application.Tests;

/// <summary>
/// SQLite 기반 relational adapter가 file-backed durable adapter와 같은 acceptance 경로를 만족하는지 검증합니다.
/// </summary>
public sealed class SqliteOperatorExecutionAdapterTests
{
    /// <summary>
    /// 공정 시작 결과가 receipt와 outbox로 저장되고 저장소 재오픈 뒤에도 동일하게 replay되는지 검증합니다.
    /// </summary>
    [Fact]
    public async Task StartOperationAsync_should_persist_receipt_and_outbox_across_store_reload()
    {
        var databaseFilePath = CreateDatabaseFilePath();
        var now = new DateTimeOffset(2026, 4, 17, 9, 0, 0, TimeSpan.Zero);
        var endpoint = CreateEndpoint(databaseFilePath, now, out var store);
        var order = CreateProductionOrder("PO-30001");
        var operation = CreateQueuedOperation(order.Id, "OP-30001", 10);
        order.AttachOperation(operation.Id);

        store.Seed(new SqliteOperatorExecutionSeed
        {
            ProductionOrders = [order],
            OperationExecutions = [operation]
        });

        var command = new StartOperationCommandContract(
            CreateContext("CMD-30001", "CORR-30001", "KEY-30001", "operator-301", "ST-301"),
            new StartOperationPayloadContract(order.Id.ToString(), operation.Id.ToString(), 10, "EA"));

        var firstResponse = await endpoint.StartOperationAsync(command);

        Assert.True(firstResponse.Accepted);

        var reloadedEndpoint = CreateEndpoint(databaseFilePath, now.AddMinutes(5), out var reloadedStore);
        var replayResponse = await reloadedEndpoint.StartOperationAsync(command);

        Assert.Equal(firstResponse, replayResponse);
        Assert.Single(reloadedStore.Receipts);
        Assert.Contains(reloadedStore.OutboxEntries, entry => entry.EventType == nameof(OperationStartedDomainEvent));
    }

    /// <summary>
    /// 공정 완료 결과가 production actuals batch와 outbox로 저장되고 저장소 재오픈 뒤에도 유지되는지 검증합니다.
    /// </summary>
    [Fact]
    public async Task CompleteOperationAsync_should_persist_canonical_save_boundary_across_store_reload()
    {
        var databaseFilePath = CreateDatabaseFilePath();
        var now = new DateTimeOffset(2026, 4, 17, 9, 10, 0, TimeSpan.Zero);
        var endpoint = CreateEndpoint(databaseFilePath, now, out var store);
        var order = CreateProductionOrder("PO-30002");
        var operation = CreateRunningOperation(order.Id.ToString(), "OP-30002", "ST-302", 10, now.AddMinutes(-15));
        order.AttachOperation(operation.Id);

        store.Seed(new SqliteOperatorExecutionSeed
        {
            ProductionOrders = [order],
            OperationExecutions = [operation]
        });

        var response = await endpoint.CompleteOperationAsync(
            new CompleteOperationCommandContract(
                CreateContext("CMD-30002", "CORR-30002", "KEY-30002", "operator-302", "ST-302"),
                new CompleteOperationPayloadContract(
                    operation.Id.ToString(),
                    new MeasuredQuantityContract(7m, "EA"),
                    new MeasuredQuantityContract(1m, "EA"),
                    CompletionModeValues.Manual)));

        Assert.True(response.Accepted);

        _ = CreateEndpoint(databaseFilePath, now.AddMinutes(10), out var reloadedStore);

        Assert.Single(reloadedStore.Receipts);
        Assert.Single(reloadedStore.ProductionActualsBatches);
        Assert.Equal(ProductionActualsStatusValues.PendingProjection, reloadedStore.ProductionActualsBatches[0].Status);
        Assert.Equal(ProductionOrderStatus.Completed, reloadedStore.GetProductionOrder(order.Id.ToString()).Status);
        Assert.Equal(OperationExecutionStatus.Done, reloadedStore.GetOperationExecution(operation.Id.ToString()).Status);
        Assert.Equal(2, reloadedStore.OutboxEntries.Count);
        Assert.Contains(reloadedStore.OutboxEntries, entry => entry.EventType == nameof(ScrapRecordedDomainEvent));
        Assert.Contains(reloadedStore.OutboxEntries, entry => entry.EventType == nameof(OperationCompletedDomainEvent));
    }

    /// <summary>
    /// replay된 `complete-operation`이 store reopen 이후에도 canonical save boundary를 중복 저장하지 않는지 검증합니다.
    /// </summary>
    [Fact]
    public async Task CompleteOperationAsync_should_replay_without_duplicate_save_boundary_after_store_reopen()
    {
        var databaseFilePath = CreateDatabaseFilePath();
        var now = new DateTimeOffset(2026, 4, 17, 9, 15, 0, TimeSpan.Zero);
        var endpoint = CreateEndpoint(databaseFilePath, now, out var store);
        var order = CreateProductionOrder("PO-30002-R");
        var operation = CreateRunningOperation(order.Id.ToString(), "OP-30002-R", "ST-302-R", 10, now.AddMinutes(-15));
        order.AttachOperation(operation.Id);

        store.Seed(new SqliteOperatorExecutionSeed
        {
            ProductionOrders = [order],
            OperationExecutions = [operation]
        });

        var command = new CompleteOperationCommandContract(
            CreateContext("CMD-30002-R", "CORR-30002-R", "KEY-30002-R", "operator-302-R", "ST-302-R"),
            new CompleteOperationPayloadContract(
                operation.Id.ToString(),
                new MeasuredQuantityContract(7m, "EA"),
                new MeasuredQuantityContract(1m, "EA"),
                CompletionModeValues.Manual));

        var firstResponse = await endpoint.CompleteOperationAsync(command);
        var reloadedEndpoint = CreateEndpoint(databaseFilePath, now.AddMinutes(5), out var reloadedStore);

        var replayResponse = await reloadedEndpoint.CompleteOperationAsync(command);

        Assert.Equal(firstResponse, replayResponse);
        Assert.Single(reloadedStore.Receipts);
        Assert.Single(reloadedStore.ProductionActualsBatches);
        Assert.Equal(2, reloadedStore.OutboxEntries.Count);
        Assert.Contains(reloadedStore.OutboxEntries, entry => entry.EventType == nameof(ScrapRecordedDomainEvent));
        Assert.Contains(reloadedStore.OutboxEntries, entry => entry.EventType == nameof(OperationCompletedDomainEvent));
        Assert.Equal(ProductionOrderStatus.Completed, reloadedStore.GetProductionOrder(order.Id.ToString()).Status);
        Assert.Equal(OperationExecutionStatus.Done, reloadedStore.GetOperationExecution(operation.Id.ToString()).Status);
    }

    /// <summary>
    /// 작업 큐 조회가 SQLite 저장소에 남은 MES-side snapshot만으로 재오픈 이후에도 동일하게 구성되는지 검증합니다.
    /// </summary>
    [Fact]
    public async Task GetStationWorkQueueAsync_should_project_persisted_snapshot_after_store_reload()
    {
        var databaseFilePath = CreateDatabaseFilePath();
        var now = new DateTimeOffset(2026, 4, 17, 9, 20, 0, TimeSpan.Zero);
        var endpoint = CreateEndpoint(databaseFilePath, now, out var store);
        var stationId = new StationId("ST-303");
        var operation = CreateRunningOperation("PO-30003", "OP-30003", stationId.ToString(), 10, now.AddMinutes(-20));
        var projector = new OperationMaterialRequirementProjector();

        store.Seed(new SqliteOperatorExecutionSeed
        {
            ProductionOrders = [CreateProductionOrder("PO-30003")],
            OperationExecutions = [operation],
            MaterialRequirements = projector.ProjectFromOperationAttachment(
                new ProjectOperationMaterialRequirementsRequest(
                    operation.Id,
                    now.AddMinutes(-10),
                    [new OperationMaterialRequirementInput(1, "MAT-303", new MeasuredQuantity(5m, "EA"), "BOM-303")]))
        });

        var reloadedEndpoint = CreateEndpoint(databaseFilePath, now.AddMinutes(1), out _);

        var response = await reloadedEndpoint.GetStationWorkQueueAsync(
            new GetStationWorkQueueRequestContract(stationId.ToString()));

        Assert.Single(response.Items);
        Assert.Equal(operation.Id.ToString(), response.Items[0].OperationExecutionId);
        Assert.Equal("MAT-303", response.Items[0].RequiredMaterials[0].MaterialCode);
        Assert.Equal(QualityGateStateValues.Open, response.Items[0].QualityGateState);
    }

    /// <summary>
    /// 테스트용 SQLite endpoint facade를 생성합니다.
    /// </summary>
    /// <param name="databaseFilePath">SQLite 데이터베이스 파일 경로입니다.</param>
    /// <param name="now">고정 서버 UTC 시각입니다.</param>
    /// <param name="store">생성한 SQLite 저장소를 반환합니다.</param>
    /// <returns>구성된 endpoint facade입니다.</returns>
    private static OperatorExecutionBffEndpointAdapter CreateEndpoint(
        string databaseFilePath,
        DateTimeOffset now,
        out SqliteOperatorExecutionStore store)
    {
        store = new SqliteOperatorExecutionStore(new SqliteOperatorExecutionStoreOptions
        {
            DatabaseFilePath = databaseFilePath
        });

        var commandPort = new SqliteOperatorExecutionCommandAdapter(store, new CanonicalCommandFingerprintBuilder());
        var sourcePort = new SqliteStationWorkQueueSourceAdapter(store);
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
    /// 테스트용 SQLite 데이터베이스 파일 경로를 생성합니다.
    /// </summary>
    /// <returns>충돌하지 않는 데이터베이스 파일 경로입니다.</returns>
    private static string CreateDatabaseFilePath()
    {
        return Path.Combine(
            Path.GetTempPath(),
            "Modbus",
            "Mes.Application.Tests",
            Guid.NewGuid().ToString("N"),
            "operator-execution.db");
    }

    /// <summary>
    /// 테스트용 command context를 생성합니다.
    /// </summary>
    /// <param name="commandId">명령 식별자입니다.</param>
    /// <param name="correlationId">상관 관계 식별자입니다.</param>
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
            new DateTimeOffset(2026, 4, 17, 8, 55, 0, TimeSpan.Zero),
            null);
    }

    /// <summary>
    /// 테스트용 생산 오더를 생성합니다.
    /// </summary>
    /// <param name="productionOrderId">생산 오더 식별자입니다.</param>
    /// <returns>released 상태의 생산 오더입니다.</returns>
    private static ProductionOrder CreateProductionOrder(string productionOrderId)
    {
        return ProductionOrder.Release(
            new ProductionOrderId(productionOrderId),
            "ITEM-" + productionOrderId,
            "ROUTE-A",
            new DateTimeOffset(2026, 4, 17, 8, 0, 0, TimeSpan.Zero));
    }

    /// <summary>
    /// 테스트용 queued 공정 실행 aggregate를 생성합니다.
    /// </summary>
    /// <param name="productionOrderId">상위 생산 오더 식별자입니다.</param>
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
    /// <param name="productionOrderId">상위 생산 오더 식별자입니다.</param>
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
    /// 고정된 현재 시각을 반환하는 테스트용 time provider입니다.
    /// </summary>
    /// <param name="utcNow">반환할 현재 UTC 시각입니다.</param>
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
