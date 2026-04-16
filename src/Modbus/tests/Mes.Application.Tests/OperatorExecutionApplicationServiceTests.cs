using Mes.Application.Contracts.Common;
using Mes.Application.Contracts.OperatorExecution;
using Mes.Application.Idempotency;
using Mes.Application.OperatorExecution;
using Mes.Application.OperatorExecution.WorkQueue;
using Mes.Domain.Aggregates;
using Mes.Domain.Entities;
using Mes.Domain.Statuses;
using Mes.Domain.ValueObjects;

namespace Mes.Application.Tests;

/// <summary>
/// 운영자 실행 application service의 로드, 저장, replay 오케스트레이션을 검증합니다.
/// </summary>
public sealed class OperatorExecutionApplicationServiceTests
{
    /// <summary>
    /// 공정 시작 요청이 상태를 로드하고 수락 결과를 저장하는지 검증합니다.
    /// </summary>
    [Fact]
    public async Task HandleAsync_start_operation_should_load_state_and_persist_result()
    {
        var serverReceivedAt = new DateTimeOffset(2026, 4, 16, 17, 20, 0, TimeSpan.Zero);
        var commandPort = new FakeOperatorExecutionCommandPort();
        var sourcePort = new FakeStationWorkQueueSourcePort();
        var service = CreateService(commandPort, sourcePort);
        var order = CreateProductionOrder("PO-9001");
        var operation = CreateQueuedOperation(order.Id, "OP-9001", 10);
        order.AttachOperation(operation.Id);
        commandPort.StartOperationState = new StartOperationCommandState(order, operation);

        var command = new StartOperationCommandContract(
            CreateContext("CMD-9001", "CORR-9001", "KEY-9001", "operator-91", "ST-91"),
            new StartOperationPayloadContract(order.Id.ToString(), operation.Id.ToString(), 10, "EA"));

        var result = await service.HandleAsync(
            new ExecuteOperatorExecutionCommandRequest<StartOperationCommandContract>(
                command,
                serverReceivedAt));

        Assert.Equal(CommandReceiptDecisionKind.AcceptNew, result.Decision);
        Assert.Equal(1, commandPort.SaveCount);
        Assert.NotNull(commandPort.LastSavedRequest);
        Assert.Equal(OperationExecutionStatus.Running, operation.Status);
        Assert.Equal(ProductionOrderStatus.InProgress, order.Status);
    }

    /// <summary>
    /// replay 가능한 품질 결과 요청은 저장 없이 기존 응답만 재생하는지 검증합니다.
    /// </summary>
    [Fact]
    public async Task HandleAsync_quality_result_should_skip_save_for_replay()
    {
        var serverReceivedAt = new DateTimeOffset(2026, 4, 16, 17, 30, 0, TimeSpan.Zero);
        var commandPort = new FakeOperatorExecutionCommandPort();
        var sourcePort = new FakeStationWorkQueueSourcePort();
        var service = CreateService(commandPort, sourcePort);
        var operation = CreateRunningOperation("PO-9002", "OP-9002", "ST-92", 10, serverReceivedAt.AddMinutes(-10));
        var wipUnit = new WipUnit(new WipUnitId("WIP-9002"), "ITEM-9002");
        wipUnit.StartProcessing(operation.Id);
        var qualityRecord = QualityRecord.Create(new QualityRecordId("QR-9002"), wipUnit.Id, "INSP-92");
        qualityRecord.BeginInspection();
        commandPort.RecordQualityResultState = new RecordQualityResultCommandState(qualityRecord, wipUnit, operation);

        var command = new RecordQualityResultCommandContract(
            CreateContext("CMD-9002", "CORR-9002", "KEY-9002", "operator-92", "ST-92"),
            new RecordQualityResultPayloadContract(
                qualityRecord.Id.ToString(),
                wipUnit.Id.ToString(),
                qualityRecord.InspectionCode,
                QualityDecisionValues.Failed,
                "dimension mismatch"));

        commandPort.ReceiptToReturn = CreateReplayReceipt(command, qualityRecord.Id.ToString(), serverReceivedAt);

        var result = await service.HandleAsync(
            new ExecuteOperatorExecutionCommandRequest<RecordQualityResultCommandContract>(
                command,
                serverReceivedAt));

        Assert.Equal(CommandReceiptDecisionKind.ReplayStored, result.Decision);
        Assert.Equal(0, commandPort.SaveCount);
        Assert.Null(commandPort.LastSavedRequest);
        Assert.Equal("Hold", result.Response.Status);
        Assert.False(result.Response.QualityGateOpen);
    }

    /// <summary>
    /// 공정 완료 요청이 production actuals batch와 함께 저장되는지 검증합니다.
    /// </summary>
    [Fact]
    public async Task HandleAsync_complete_operation_should_persist_prepared_batch()
    {
        var serverReceivedAt = new DateTimeOffset(2026, 4, 16, 17, 40, 0, TimeSpan.Zero);
        var commandPort = new FakeOperatorExecutionCommandPort();
        var sourcePort = new FakeStationWorkQueueSourcePort();
        var service = CreateService(commandPort, sourcePort);
        var order = CreateProductionOrder("PO-9003");
        var operation = CreateRunningOperation(order.Id.ToString(), "OP-9003", "ST-93", 10, serverReceivedAt.AddMinutes(-10));
        order.AttachOperation(operation.Id);
        commandPort.CompleteOperationState = new CompleteOperationCommandState(order, operation);

        var command = new CompleteOperationCommandContract(
            CreateContext("CMD-9003", "CORR-9003", "KEY-9003", "operator-93", "ST-93"),
            new CompleteOperationPayloadContract(
                operation.Id.ToString(),
                new MeasuredQuantityContract(5m, "EA"),
                new MeasuredQuantityContract(1m, "EA"),
                CompletionModeValues.Manual));

        var result = await service.HandleAsync(
            new ExecuteOperatorExecutionCommandRequest<CompleteOperationCommandContract>(
                command,
                serverReceivedAt));

        Assert.Equal(CommandReceiptDecisionKind.AcceptNew, result.Decision);
        Assert.Equal(1, commandPort.SaveCount);
        var savedRequest = Assert.IsType<PersistOperatorExecutionCommandRequest<CompleteOperationCommandState>>(commandPort.LastSavedRequest);
        Assert.NotNull(savedRequest.PreparedBatch);
        Assert.Equal(ProductionActualsStatusValues.PendingProjection, savedRequest.PreparedBatch!.Status);
    }

    /// <summary>
    /// 작업 큐 query가 source 포트만 사용해 응답을 조합하는지 검증합니다.
    /// </summary>
    [Fact]
    public async Task HandleAsync_work_queue_should_load_source_and_map_response()
    {
        var snapshotTakenAt = new DateTimeOffset(2026, 4, 16, 17, 50, 0, TimeSpan.Zero);
        var commandPort = new FakeOperatorExecutionCommandPort();
        var sourcePort = new FakeStationWorkQueueSourcePort();
        var service = CreateService(commandPort, sourcePort);
        var stationId = new StationId("ST-94");
        var operation = CreateRunningOperation("PO-9004", "OP-9004", stationId.ToString(), 10, snapshotTakenAt.AddMinutes(-20));
        var projector = new OperationMaterialRequirementProjector();

        sourcePort.SourceToReturn = new StationWorkQueueSource(
            [operation],
            [],
            [],
            projector.ProjectFromOperationAttachment(
                new ProjectOperationMaterialRequirementsRequest(
                    operation.Id,
                    snapshotTakenAt.AddMinutes(-10),
                    [new OperationMaterialRequirementInput(1, "MAT-94", new MeasuredQuantity(2m, "EA"), "BOM-94")])));

        var response = await service.HandleAsync(
            new ExecuteStationWorkQueueQueryRequest(
                new GetStationWorkQueueRequestContract(stationId.ToString()),
                snapshotTakenAt));

        Assert.Equal(1, sourcePort.LoadCount);
        Assert.Single(response.Items);
        Assert.Equal("MAT-94", response.Items[0].RequiredMaterials[0].MaterialCode);
        Assert.Equal(QualityGateStateValues.Open, response.Items[0].QualityGateState);
    }

    /// <summary>
    /// 테스트용 application service를 생성합니다.
    /// </summary>
    /// <param name="commandPort">command 포트입니다.</param>
    /// <param name="sourcePort">작업 큐 source 포트입니다.</param>
    /// <returns>기본 의존성으로 구성한 application service입니다.</returns>
    private static OperatorExecutionApplicationService CreateService(
        FakeOperatorExecutionCommandPort commandPort,
        FakeStationWorkQueueSourcePort sourcePort)
    {
        return new OperatorExecutionApplicationService(
            new OperatorExecutionCommandHandler(
                new CanonicalCommandFingerprintBuilder(),
                new CommandReceiptIdempotencyPolicy(),
                new StoredCommandResponseSerializer(),
                new QualityHoldGateCoordinator(),
                new ProductionActualsPreparationService()),
            commandPort,
            new GetStationWorkQueueQueryHandler(new StationWorkQueueReadService()),
            sourcePort);
    }

    /// <summary>
    /// replay용 receipt를 생성합니다.
    /// </summary>
    /// <param name="command">품질 결과 command입니다.</param>
    /// <param name="aggregateId">대상 aggregate 식별자입니다.</param>
    /// <param name="acceptedAt">receipt 수락 시각입니다.</param>
    /// <returns>저장 응답을 포함한 replay receipt입니다.</returns>
    private static CommandReceiptRecord CreateReplayReceipt(
        RecordQualityResultCommandContract command,
        string aggregateId,
        DateTimeOffset acceptedAt)
    {
        var builder = new CanonicalCommandFingerprintBuilder();
        var serializer = new StoredCommandResponseSerializer();
        var policy = new CommandReceiptIdempotencyPolicy();
        var response = new RecordQualityResultResponseContract(
            true,
            command.CommandId,
            acceptedAt,
            command.Payload.QualityRecordId,
            "Hold",
            false);

        return policy.CreateAcceptedReceipt(
            new RegisterAcceptedCommandReceiptRequest(
                command.CommandId,
                builder.CreateScope(command),
                command.ActorId,
                command.StationId,
                command.CorrelationId,
                builder.Build(command),
                nameof(QualityRecord),
                aggregateId,
                acceptedAt,
                serializer.Serialize(response)));
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
            new DateTimeOffset(2026, 4, 16, 17, 0, 0, TimeSpan.Zero),
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
            new DateTimeOffset(2026, 4, 16, 16, 0, 0, TimeSpan.Zero));
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
    /// 운영자 실행 command 포트 테스트 대역입니다.
    /// </summary>
    private sealed class FakeOperatorExecutionCommandPort : IOperatorExecutionCommandPort
    {
        /// <summary>
        /// replay에 사용할 receipt입니다.
        /// </summary>
        public CommandReceiptRecord? ReceiptToReturn { get; set; }

        /// <summary>
        /// 공정 시작용 상태입니다.
        /// </summary>
        public StartOperationCommandState? StartOperationState { get; set; }

        /// <summary>
        /// 자재 소모용 상태입니다.
        /// </summary>
        public RecordMaterialConsumptionCommandState? RecordMaterialConsumptionState { get; set; }

        /// <summary>
        /// hold 설정용 상태입니다.
        /// </summary>
        public PlaceHoldCommandState? PlaceHoldState { get; set; }

        /// <summary>
        /// hold 해제용 상태입니다.
        /// </summary>
        public ReleaseHoldCommandState? ReleaseHoldState { get; set; }

        /// <summary>
        /// 품질 결과용 상태입니다.
        /// </summary>
        public RecordQualityResultCommandState? RecordQualityResultState { get; set; }

        /// <summary>
        /// 공정 완료용 상태입니다.
        /// </summary>
        public CompleteOperationCommandState? CompleteOperationState { get; set; }

        /// <summary>
        /// 마지막 저장 요청입니다.
        /// </summary>
        public object? LastSavedRequest { get; private set; }

        /// <summary>
        /// 저장 호출 횟수입니다.
        /// </summary>
        public int SaveCount { get; private set; }

        /// <summary>
        /// 기존 receipt를 반환합니다.
        /// </summary>
        /// <typeparam name="TPayload">command payload 형식입니다.</typeparam>
        /// <param name="command">조회 대상 command입니다.</param>
        /// <param name="cancellationToken">취소 토큰입니다.</param>
        /// <returns>설정된 receipt입니다.</returns>
        public Task<CommandReceiptRecord?> LoadReceiptAsync<TPayload>(BffCommandEnvelope<TPayload> command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ReceiptToReturn);
        }

        /// <summary>
        /// 공정 시작 상태를 반환합니다.
        /// </summary>
        /// <param name="command">공정 시작 command입니다.</param>
        /// <param name="cancellationToken">취소 토큰입니다.</param>
        /// <returns>설정된 상태입니다.</returns>
        public Task<StartOperationCommandState> LoadStateAsync(StartOperationCommandContract command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StartOperationState ?? throw new InvalidOperationException("StartOperationState is not configured."));
        }

        /// <summary>
        /// 자재 소모 상태를 반환합니다.
        /// </summary>
        /// <param name="command">자재 소모 command입니다.</param>
        /// <param name="cancellationToken">취소 토큰입니다.</param>
        /// <returns>설정된 상태입니다.</returns>
        public Task<RecordMaterialConsumptionCommandState> LoadStateAsync(RecordMaterialConsumptionCommandContract command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(RecordMaterialConsumptionState ?? throw new InvalidOperationException("RecordMaterialConsumptionState is not configured."));
        }

        /// <summary>
        /// hold 설정 상태를 반환합니다.
        /// </summary>
        /// <param name="command">hold 설정 command입니다.</param>
        /// <param name="cancellationToken">취소 토큰입니다.</param>
        /// <returns>설정된 상태입니다.</returns>
        public Task<PlaceHoldCommandState> LoadStateAsync(PlaceHoldCommandContract command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PlaceHoldState ?? throw new InvalidOperationException("PlaceHoldState is not configured."));
        }

        /// <summary>
        /// hold 해제 상태를 반환합니다.
        /// </summary>
        /// <param name="command">hold 해제 command입니다.</param>
        /// <param name="cancellationToken">취소 토큰입니다.</param>
        /// <returns>설정된 상태입니다.</returns>
        public Task<ReleaseHoldCommandState> LoadStateAsync(ReleaseHoldCommandContract command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ReleaseHoldState ?? throw new InvalidOperationException("ReleaseHoldState is not configured."));
        }

        /// <summary>
        /// 품질 결과 상태를 반환합니다.
        /// </summary>
        /// <param name="command">품질 결과 command입니다.</param>
        /// <param name="cancellationToken">취소 토큰입니다.</param>
        /// <returns>설정된 상태입니다.</returns>
        public Task<RecordQualityResultCommandState> LoadStateAsync(RecordQualityResultCommandContract command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(RecordQualityResultState ?? throw new InvalidOperationException("RecordQualityResultState is not configured."));
        }

        /// <summary>
        /// 공정 완료 상태를 반환합니다.
        /// </summary>
        /// <param name="command">공정 완료 command입니다.</param>
        /// <param name="cancellationToken">취소 토큰입니다.</param>
        /// <returns>설정된 상태입니다.</returns>
        public Task<CompleteOperationCommandState> LoadStateAsync(CompleteOperationCommandContract command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CompleteOperationState ?? throw new InvalidOperationException("CompleteOperationState is not configured."));
        }

        /// <summary>
        /// 마지막 저장 요청을 기록합니다.
        /// </summary>
        /// <typeparam name="TState">저장 요청 상태 형식입니다.</typeparam>
        /// <param name="request">저장 요청입니다.</param>
        /// <param name="cancellationToken">취소 토큰입니다.</param>
        /// <returns>완료된 작업입니다.</returns>
        public Task SaveAsync<TState>(PersistOperatorExecutionCommandRequest<TState> request, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            LastSavedRequest = request;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 작업 큐 source 포트 테스트 대역입니다.
    /// </summary>
    private sealed class FakeStationWorkQueueSourcePort : IStationWorkQueueSourcePort
    {
        /// <summary>
        /// 반환할 source snapshot입니다.
        /// </summary>
        public StationWorkQueueSource? SourceToReturn { get; set; }

        /// <summary>
        /// 로드 호출 횟수입니다.
        /// </summary>
        public int LoadCount { get; private set; }

        /// <summary>
        /// 설정된 source snapshot을 반환합니다.
        /// </summary>
        /// <param name="request">작업 큐 query 계약입니다.</param>
        /// <param name="cancellationToken">취소 토큰입니다.</param>
        /// <returns>설정된 source snapshot입니다.</returns>
        public Task<StationWorkQueueSource> LoadSourceAsync(GetStationWorkQueueRequestContract request, CancellationToken cancellationToken = default)
        {
            LoadCount++;
            return Task.FromResult(SourceToReturn ?? throw new InvalidOperationException("SourceToReturn is not configured."));
        }
    }
}
