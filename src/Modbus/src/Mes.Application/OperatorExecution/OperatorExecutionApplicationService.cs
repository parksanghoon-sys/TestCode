using Mes.Application.Contracts.OperatorExecution;
using Mes.Application.OperatorExecution.WorkQueue;

namespace Mes.Application.OperatorExecution;

/// <summary>
/// 운영자 실행 slice의 로드, handler 호출, 저장 오케스트레이션을 담당합니다.
/// </summary>
public sealed class OperatorExecutionApplicationService
{
    private readonly OperatorExecutionCommandHandler _commandHandler;
    private readonly IOperatorExecutionCommandPort _commandPort;
    private readonly GetStationWorkQueueQueryHandler _workQueueQueryHandler;
    private readonly IStationWorkQueueSourcePort _workQueueSourcePort;

    /// <summary>
    /// 운영자 실행 application service를 초기화합니다.
    /// </summary>
    /// <param name="commandHandler">순수 command handler입니다.</param>
    /// <param name="commandPort">command 상태 로드/저장 포트입니다.</param>
    /// <param name="workQueueQueryHandler">작업 큐 query handler입니다.</param>
    /// <param name="workQueueSourcePort">작업 큐 source 로드 포트입니다.</param>
    public OperatorExecutionApplicationService(
        OperatorExecutionCommandHandler commandHandler,
        IOperatorExecutionCommandPort commandPort,
        GetStationWorkQueueQueryHandler workQueueQueryHandler,
        IStationWorkQueueSourcePort workQueueSourcePort)
    {
        _commandHandler = commandHandler;
        _commandPort = commandPort;
        _workQueueQueryHandler = workQueueQueryHandler;
        _workQueueSourcePort = workQueueSourcePort;
    }

    /// <summary>
    /// `start-operation` command를 오케스트레이션합니다.
    /// </summary>
    /// <param name="request">실행 요청입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>처리 결과입니다.</returns>
    public async Task<HandledCommandResult<StartOperationResponseContract>> HandleAsync(
        ExecuteOperatorExecutionCommandRequest<StartOperationCommandContract> request,
        CancellationToken cancellationToken = default)
    {
        var state = await _commandPort.LoadStateAsync(request.Command, cancellationToken);
        var receipt = await _commandPort.LoadReceiptAsync(request.Command, cancellationToken);
        var result = _commandHandler.Handle(
            new OperatorExecutionCommandHandlingRequest<StartOperationCommandContract, StartOperationCommandState>(
                request.Command,
                state,
                receipt,
                request.ServerReceivedAt));

        await PersistIfNeededAsync(state, result.ReceiptToStore, null, request.ServerReceivedAt, cancellationToken);
        return result;
    }

    /// <summary>
    /// `record-material-scan` command를 오케스트레이션합니다.
    /// </summary>
    /// <param name="request">실행 요청입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>처리 결과입니다.</returns>
    public async Task<HandledCommandResult<RecordMaterialScanResponseContract>> HandleAsync(
        ExecuteOperatorExecutionCommandRequest<RecordMaterialScanCommandContract> request,
        CancellationToken cancellationToken = default)
    {
        var state = await _commandPort.LoadStateAsync(request.Command, cancellationToken);
        var receipt = await _commandPort.LoadReceiptAsync(request.Command, cancellationToken);
        var result = _commandHandler.Handle(
            new OperatorExecutionCommandHandlingRequest<RecordMaterialScanCommandContract, RecordMaterialScanCommandState>(
                request.Command,
                state,
                receipt,
                request.ServerReceivedAt));

        await PersistIfNeededAsync(state, result.ReceiptToStore, null, request.ServerReceivedAt, cancellationToken);
        return result;
    }

    /// <summary>
    /// `record-material-consumption` command를 오케스트레이션합니다.
    /// </summary>
    /// <param name="request">실행 요청입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>처리 결과입니다.</returns>
    public async Task<HandledCommandResult<RecordMaterialConsumptionResponseContract>> HandleAsync(
        ExecuteOperatorExecutionCommandRequest<RecordMaterialConsumptionCommandContract> request,
        CancellationToken cancellationToken = default)
    {
        var state = await _commandPort.LoadStateAsync(request.Command, cancellationToken);
        var receipt = await _commandPort.LoadReceiptAsync(request.Command, cancellationToken);
        var result = _commandHandler.Handle(
            new OperatorExecutionCommandHandlingRequest<RecordMaterialConsumptionCommandContract, RecordMaterialConsumptionCommandState>(
                request.Command,
                state,
                receipt,
                request.ServerReceivedAt));

        await PersistIfNeededAsync(state, result.ReceiptToStore, null, request.ServerReceivedAt, cancellationToken);
        return result;
    }

    /// <summary>
    /// `place-hold` command를 오케스트레이션합니다.
    /// </summary>
    /// <param name="request">실행 요청입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>처리 결과입니다.</returns>
    public async Task<HandledCommandResult<PlaceHoldResponseContract>> HandleAsync(
        ExecuteOperatorExecutionCommandRequest<PlaceHoldCommandContract> request,
        CancellationToken cancellationToken = default)
    {
        var state = await _commandPort.LoadStateAsync(request.Command, cancellationToken);
        var receipt = await _commandPort.LoadReceiptAsync(request.Command, cancellationToken);
        var result = _commandHandler.Handle(
            new OperatorExecutionCommandHandlingRequest<PlaceHoldCommandContract, PlaceHoldCommandState>(
                request.Command,
                state,
                receipt,
                request.ServerReceivedAt));

        await PersistIfNeededAsync(state, result.ReceiptToStore, null, request.ServerReceivedAt, cancellationToken);
        return result;
    }

    /// <summary>
    /// `release-hold` command를 오케스트레이션합니다.
    /// </summary>
    /// <param name="request">실행 요청입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>처리 결과입니다.</returns>
    public async Task<HandledCommandResult<ReleaseHoldResponseContract>> HandleAsync(
        ExecuteOperatorExecutionCommandRequest<ReleaseHoldCommandContract> request,
        CancellationToken cancellationToken = default)
    {
        var state = await _commandPort.LoadStateAsync(request.Command, cancellationToken);
        var receipt = await _commandPort.LoadReceiptAsync(request.Command, cancellationToken);
        var result = _commandHandler.Handle(
            new OperatorExecutionCommandHandlingRequest<ReleaseHoldCommandContract, ReleaseHoldCommandState>(
                request.Command,
                state,
                receipt,
                request.ServerReceivedAt));

        await PersistIfNeededAsync(state, result.ReceiptToStore, null, request.ServerReceivedAt, cancellationToken);
        return result;
    }

    /// <summary>
    /// `record-quality-result` command를 오케스트레이션합니다.
    /// </summary>
    /// <param name="request">실행 요청입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>처리 결과입니다.</returns>
    public async Task<HandledCommandResult<RecordQualityResultResponseContract>> HandleAsync(
        ExecuteOperatorExecutionCommandRequest<RecordQualityResultCommandContract> request,
        CancellationToken cancellationToken = default)
    {
        var state = await _commandPort.LoadStateAsync(request.Command, cancellationToken);
        var receipt = await _commandPort.LoadReceiptAsync(request.Command, cancellationToken);
        var result = _commandHandler.Handle(
            new OperatorExecutionCommandHandlingRequest<RecordQualityResultCommandContract, RecordQualityResultCommandState>(
                request.Command,
                state,
                receipt,
                request.ServerReceivedAt));

        await PersistIfNeededAsync(state, result.ReceiptToStore, null, request.ServerReceivedAt, cancellationToken);
        return result;
    }

    /// <summary>
    /// `complete-operation` command를 오케스트레이션합니다.
    /// </summary>
    /// <param name="request">실행 요청입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>처리 결과입니다.</returns>
    public async Task<CompleteOperationHandledCommandResult> HandleAsync(
        ExecuteOperatorExecutionCommandRequest<CompleteOperationCommandContract> request,
        CancellationToken cancellationToken = default)
    {
        var state = await _commandPort.LoadStateAsync(request.Command, cancellationToken);
        var receipt = await _commandPort.LoadReceiptAsync(request.Command, cancellationToken);
        var result = _commandHandler.Handle(
            new OperatorExecutionCommandHandlingRequest<CompleteOperationCommandContract, CompleteOperationCommandState>(
                request.Command,
                state,
                receipt,
                request.ServerReceivedAt));

        await PersistIfNeededAsync(state, result.ReceiptToStore, result.PreparedBatch, request.ServerReceivedAt, cancellationToken);
        return result;
    }

    /// <summary>
    /// `GetStationWorkQueue` query를 오케스트레이션합니다.
    /// </summary>
    /// <param name="request">실행 요청입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>작업 큐 응답입니다.</returns>
    public async Task<GetStationWorkQueueResponseContract> HandleAsync(
        ExecuteStationWorkQueueQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var source = await _workQueueSourcePort.LoadSourceAsync(request.Request, cancellationToken);
        return _workQueueQueryHandler.Handle(
            new GetStationWorkQueueHandlingRequest(
                request.Request,
                source,
                request.SnapshotTakenAt));
    }

    /// <summary>
    /// 새로 수락된 command 결과가 있으면 상태와 receipt를 저장합니다.
    /// </summary>
    /// <typeparam name="TState">저장할 상태 묶음 형식입니다.</typeparam>
    /// <param name="state">저장할 상태입니다.</param>
    /// <param name="receiptToStore">저장할 receipt입니다.</param>
    /// <param name="preparedBatch">저장할 production actuals batch입니다.</param>
    /// <param name="persistedAt">저장 기준 시각입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>비동기 저장 작업입니다.</returns>
    private Task PersistIfNeededAsync<TState>(
        TState state,
        Mes.Application.Idempotency.CommandReceiptRecord? receiptToStore,
        PreparedProductionActualsBatch? preparedBatch,
        DateTimeOffset persistedAt,
        CancellationToken cancellationToken)
    {
        if (receiptToStore is null && preparedBatch is null)
        {
            return Task.CompletedTask;
        }

        return _commandPort.SaveAsync(
            new PersistOperatorExecutionCommandRequest<TState>(
                state,
                receiptToStore,
                preparedBatch,
                persistedAt),
            cancellationToken);
    }
}
