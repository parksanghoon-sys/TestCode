using Mes.Application.Contracts.Common;
using Mes.Application.Contracts.OperatorExecution;
using Mes.Application.Idempotency;
using Mes.Domain.Aggregates;
using Mes.Domain.Entities;

namespace Mes.Application.OperatorExecution;

/// <summary>
/// operator-execution 명령 handler 가 공통으로 받는 입력 묶음입니다.
/// </summary>
/// <typeparam name="TCommand">처리할 명령 계약 형식입니다.</typeparam>
/// <typeparam name="TState">명령 처리에 필요한 aggregate 상태 묶음입니다.</typeparam>
/// <param name="Command">처리할 명령 계약입니다.</param>
/// <param name="State">명령 처리에 필요한 aggregate 상태입니다.</param>
/// <param name="ExistingReceipt">같은 scope 로 저장된 기존 receipt 입니다.</param>
/// <param name="ServerReceivedAt">서버가 명령을 수신한 시각입니다.</param>
public sealed record OperatorExecutionCommandHandlingRequest<TCommand, TState>(
    TCommand Command,
    TState State,
    CommandReceiptRecord? ExistingReceipt,
    DateTimeOffset ServerReceivedAt);

/// <summary>
/// 일반 command handler 처리 결과를 나타냅니다.
/// </summary>
/// <typeparam name="TResponse">응답 계약 형식입니다.</typeparam>
/// <param name="Decision">idempotency 처리 결과입니다.</param>
/// <param name="Response">호출자에게 반환할 응답입니다.</param>
/// <param name="ReceiptToStore">새로 저장해야 하는 receipt 입니다.</param>
public sealed record HandledCommandResult<TResponse>(
    CommandReceiptDecisionKind Decision,
    TResponse Response,
    CommandReceiptRecord? ReceiptToStore);

/// <summary>
/// 공정 완료 handler 가 production actuals skeleton 과 함께 반환하는 결과입니다.
/// </summary>
/// <param name="Decision">idempotency 처리 결과입니다.</param>
/// <param name="Response">호출자에게 반환할 완료 응답입니다.</param>
/// <param name="ReceiptToStore">새로 저장해야 하는 receipt 입니다.</param>
/// <param name="PreparedBatch">준비된 production actuals batch skeleton 입니다.</param>
public sealed record CompleteOperationHandledCommandResult(
    CommandReceiptDecisionKind Decision,
    CompleteOperationResponseContract Response,
    CommandReceiptRecord? ReceiptToStore,
    PreparedProductionActualsBatch PreparedBatch);

/// <summary>
/// 공정 시작에 필요한 aggregate 상태 묶음입니다.
/// </summary>
/// <param name="ProductionOrder">대상 생산 오더입니다.</param>
/// <param name="OperationExecution">대상 공정 실행입니다.</param>
public sealed record StartOperationCommandState(
    ProductionOrder ProductionOrder,
    OperationExecution OperationExecution);

/// <summary>
/// 자재 소모 확정에 필요한 aggregate 상태 묶음입니다.
/// </summary>
/// <param name="OperationExecution">활성 공정 실행입니다.</param>
/// <param name="WipUnit">대상 WIP 입니다.</param>
/// <param name="MaterialLot">소모 대상 자재 lot 입니다.</param>
public sealed record RecordMaterialConsumptionCommandState(
    OperationExecution OperationExecution,
    WipUnit WipUnit,
    MaterialLot MaterialLot);

/// <summary>
/// hold 설정에 필요한 aggregate 상태 묶음입니다.
/// </summary>
/// <param name="OperationExecution">대상 공정 실행입니다.</param>
/// <param name="WipUnit">대상 WIP 입니다.</param>
/// <param name="QualityRecord">대상 품질 기록입니다.</param>
/// <param name="LinkedOperationExecution">품질 기록과 함께 동기화할 공정 실행입니다.</param>
public sealed record PlaceHoldCommandState(
    OperationExecution? OperationExecution,
    WipUnit? WipUnit,
    QualityRecord? QualityRecord,
    OperationExecution? LinkedOperationExecution = null);

/// <summary>
/// hold 해제에 필요한 aggregate 상태 묶음입니다.
/// </summary>
/// <param name="OperationExecution">대상 공정 실행입니다.</param>
/// <param name="WipUnit">대상 WIP 입니다.</param>
/// <param name="QualityRecord">대상 품질 기록입니다.</param>
/// <param name="LinkedOperationExecution">품질 기록과 함께 동기화할 공정 실행입니다.</param>
public sealed record ReleaseHoldCommandState(
    OperationExecution? OperationExecution,
    WipUnit? WipUnit,
    QualityRecord? QualityRecord,
    OperationExecution? LinkedOperationExecution = null);

/// <summary>
/// 품질 판정 기록에 필요한 aggregate 상태 묶음입니다.
/// </summary>
/// <param name="QualityRecord">대상 품질 기록입니다.</param>
/// <param name="WipUnit">검사 대상 WIP 입니다.</param>
/// <param name="OperationExecution">현재 공정 실행입니다.</param>
public sealed record RecordQualityResultCommandState(
    QualityRecord QualityRecord,
    WipUnit WipUnit,
    OperationExecution OperationExecution);

/// <summary>
/// 공정 완료에 필요한 aggregate 상태 묶음입니다.
/// </summary>
/// <param name="ProductionOrder">상위 생산 오더입니다.</param>
/// <param name="OperationExecution">완료 대상 공정 실행입니다.</param>
public sealed record CompleteOperationCommandState(
    ProductionOrder ProductionOrder,
    OperationExecution OperationExecution);

/// <summary>
/// 작업 큐 query handler 가 받는 입력 묶음입니다.
/// </summary>
/// <param name="Request">작업 큐 요청 계약입니다.</param>
/// <param name="Source">작업 큐를 구성할 MES-side source 입니다.</param>
/// <param name="SnapshotTakenAt">snapshot 생성 시각입니다.</param>
public sealed record GetStationWorkQueueHandlingRequest(
    GetStationWorkQueueRequestContract Request,
    WorkQueue.StationWorkQueueSource Source,
    DateTimeOffset SnapshotTakenAt);
