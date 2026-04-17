using Mes.Application.Contracts.Common;
using Mes.Application.Contracts.OperatorExecution;
using Mes.Application.Idempotency;
using Mes.Application.OperatorExecution.WorkQueue;

namespace Mes.Application.OperatorExecution;

/// <summary>
/// 운영자 실행 command 처리에 필요한 상태 로드와 결과 저장 포트를 정의합니다.
/// </summary>
public interface IOperatorExecutionCommandPort
{
    /// <summary>
    /// 현재 command scope에 해당하는 기존 receipt를 조회합니다.
    /// </summary>
    /// <typeparam name="TPayload">command payload 형식입니다.</typeparam>
    /// <param name="command">receipt를 찾을 command envelope입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>기존 receipt가 있으면 반환합니다.</returns>
    Task<CommandReceiptRecord?> LoadReceiptAsync<TPayload>(
        BffCommandEnvelope<TPayload> command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 공정 시작 처리에 필요한 상태를 로드합니다.
    /// </summary>
    /// <param name="command">공정 시작 command입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>시작 처리용 상태 묶음입니다.</returns>
    Task<StartOperationCommandState> LoadStateAsync(
        StartOperationCommandContract command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 자재 스캔 검증 처리에 필요한 상태를 로드합니다.
    /// </summary>
    /// <param name="command">자재 스캔 검증 command입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>자재 스캔 검증 처리 상태 묶음입니다.</returns>
    Task<RecordMaterialScanCommandState> LoadStateAsync(
        RecordMaterialScanCommandContract command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 자재 소모 처리에 필요한 상태를 로드합니다.
    /// </summary>
    /// <param name="command">자재 소모 command입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>자재 소모 처리용 상태 묶음입니다.</returns>
    Task<RecordMaterialConsumptionCommandState> LoadStateAsync(
        RecordMaterialConsumptionCommandContract command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// hold 설정 처리에 필요한 상태를 로드합니다.
    /// </summary>
    /// <param name="command">hold 설정 command입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>hold 설정 처리용 상태 묶음입니다.</returns>
    Task<PlaceHoldCommandState> LoadStateAsync(
        PlaceHoldCommandContract command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// hold 해제 처리에 필요한 상태를 로드합니다.
    /// </summary>
    /// <param name="command">hold 해제 command입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>hold 해제 처리용 상태 묶음입니다.</returns>
    Task<ReleaseHoldCommandState> LoadStateAsync(
        ReleaseHoldCommandContract command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 품질 결과 기록 처리에 필요한 상태를 로드합니다.
    /// </summary>
    /// <param name="command">품질 결과 command입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>품질 결과 처리용 상태 묶음입니다.</returns>
    Task<RecordQualityResultCommandState> LoadStateAsync(
        RecordQualityResultCommandContract command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 공정 완료 처리에 필요한 상태를 로드합니다.
    /// </summary>
    /// <param name="command">공정 완료 command입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>공정 완료 처리용 상태 묶음입니다.</returns>
    Task<CompleteOperationCommandState> LoadStateAsync(
        CompleteOperationCommandContract command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// command 처리 결과를 receipt와 함께 저장합니다.
    /// </summary>
    /// <typeparam name="TState">저장할 상태 묶음 형식입니다.</typeparam>
    /// <param name="request">저장할 상태와 부가 결과입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>비동기 저장 작업입니다.</returns>
    Task SaveAsync<TState>(
        PersistOperatorExecutionCommandRequest<TState> request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 작업 큐 query에 필요한 MES-side source 로드 포트를 정의합니다.
/// </summary>
public interface IStationWorkQueueSourcePort
{
    /// <summary>
    /// 작업 큐 query에 필요한 source snapshot을 로드합니다.
    /// </summary>
    /// <param name="request">작업 큐 query 계약입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>작업 큐 구성용 source snapshot입니다.</returns>
    Task<StationWorkQueueSource> LoadSourceAsync(
        GetStationWorkQueueRequestContract request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// command 처리 후 저장해야 하는 상태와 부가 산출물을 묶습니다.
/// </summary>
/// <typeparam name="TState">저장할 상태 묶음 형식입니다.</typeparam>
/// <param name="State">저장할 aggregate 상태 묶음입니다.</param>
/// <param name="ReceiptToStore">새로 저장할 receipt입니다.</param>
/// <param name="PreparedBatch">함께 저장할 production actuals batch skeleton입니다.</param>
/// <param name="PersistedAt">저장 기준 시각입니다.</param>
public sealed record PersistOperatorExecutionCommandRequest<TState>(
    TState State,
    CommandReceiptRecord? ReceiptToStore,
    PreparedProductionActualsBatch? PreparedBatch,
    DateTimeOffset PersistedAt);
