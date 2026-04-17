using Mes.Application.Contracts.Common;
using Mes.Application.Contracts.OperatorExecution;
using Mes.Application.Idempotency;
using Mes.Domain.Aggregates;
using Mes.Domain.Entities;

namespace Mes.Application.OperatorExecution;

/// <summary>
/// operator-execution command handler가 공통으로 받는 입력 묶음입니다.
/// </summary>
/// <typeparam name="TCommand">처리할 명령 계약 형식입니다.</typeparam>
/// <typeparam name="TState">명령 처리에 필요한 aggregate 상태 묶음입니다.</typeparam>
/// <param name="Command">처리할 명령 계약입니다.</param>
/// <param name="State">명령 처리에 필요한 aggregate 상태입니다.</param>
/// <param name="ExistingReceipt">같은 scope로 저장된 기존 receipt입니다.</param>
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
/// <param name="ReceiptToStore">새로 저장해야 하는 receipt입니다.</param>
public sealed record HandledCommandResult<TResponse>(
    CommandReceiptDecisionKind Decision,
    TResponse Response,
    CommandReceiptRecord? ReceiptToStore);

/// <summary>
/// 공정 완료 handler가 production actuals skeleton과 함께 반환하는 결과입니다.
/// </summary>
/// <param name="Decision">idempotency 처리 결과입니다.</param>
/// <param name="Response">호출자에게 반환할 완료 응답입니다.</param>
/// <param name="ReceiptToStore">새로 저장해야 하는 receipt입니다.</param>
/// <param name="PreparedBatch">준비한 production actuals batch skeleton입니다.</param>
public sealed record CompleteOperationHandledCommandResult(
    CommandReceiptDecisionKind Decision,
    CompleteOperationResponseContract Response,
    CommandReceiptRecord? ReceiptToStore,
    PreparedProductionActualsBatch PreparedBatch);

/// <summary>
/// 공정 시작 handler에 필요한 aggregate 상태 묶음입니다.
/// </summary>
/// <param name="ProductionOrder">대상 생산오더입니다.</param>
/// <param name="OperationExecution">대상 공정 실행입니다.</param>
public sealed record StartOperationCommandState(
    ProductionOrder ProductionOrder,
    OperationExecution OperationExecution);

/// <summary>
/// 자재 스캔 검증에 필요한 aggregate 상태 묶음입니다.
/// </summary>
/// <param name="OperationExecution">검증 대상 공정 실행입니다.</param>
/// <param name="WipUnit">검증 대상 WIP입니다.</param>
/// <param name="MaterialLot">검증 대상 자재 lot입니다.</param>
/// <param name="RequiredMaterialCodes">현재 공정의 요구 자재 코드 목록입니다.</param>
public sealed record RecordMaterialScanCommandState(
    OperationExecution OperationExecution,
    WipUnit WipUnit,
    MaterialLot MaterialLot,
    IReadOnlyList<string> RequiredMaterialCodes);

/// <summary>
/// 자재 투입 확정에 필요한 aggregate 상태 묶음입니다.
/// </summary>
/// <param name="OperationExecution">투입 중인 공정 실행입니다.</param>
/// <param name="WipUnit">대상 WIP입니다.</param>
/// <param name="MaterialLot">투입 대상 자재 lot입니다.</param>
public sealed record RecordMaterialConsumptionCommandState(
    OperationExecution OperationExecution,
    WipUnit WipUnit,
    MaterialLot MaterialLot);

/// <summary>
/// hold 설정에 필요한 aggregate 상태 묶음입니다.
/// </summary>
/// <param name="OperationExecution">대상 공정 실행입니다.</param>
/// <param name="WipUnit">대상 WIP입니다.</param>
/// <param name="QualityRecord">대상 품질 기록입니다.</param>
/// <param name="LinkedOperationExecution">품질 기록과 연계된 공정 실행입니다.</param>
public sealed record PlaceHoldCommandState(
    OperationExecution? OperationExecution,
    WipUnit? WipUnit,
    QualityRecord? QualityRecord,
    OperationExecution? LinkedOperationExecution = null);

/// <summary>
/// hold 해제에 필요한 aggregate 상태 묶음입니다.
/// </summary>
/// <param name="OperationExecution">대상 공정 실행입니다.</param>
/// <param name="WipUnit">대상 WIP입니다.</param>
/// <param name="QualityRecord">대상 품질 기록입니다.</param>
/// <param name="LinkedOperationExecution">품질 기록과 연계된 공정 실행입니다.</param>
public sealed record ReleaseHoldCommandState(
    OperationExecution? OperationExecution,
    WipUnit? WipUnit,
    QualityRecord? QualityRecord,
    OperationExecution? LinkedOperationExecution = null);

/// <summary>
/// 품질 판정 기록에 필요한 aggregate 상태 묶음입니다.
/// </summary>
/// <param name="QualityRecord">대상 품질 기록입니다.</param>
/// <param name="WipUnit">검사 대상 WIP입니다.</param>
/// <param name="OperationExecution">현재 공정 실행입니다.</param>
public sealed record RecordQualityResultCommandState(
    QualityRecord QualityRecord,
    WipUnit WipUnit,
    OperationExecution OperationExecution);

/// <summary>
/// 생산오더 완료 진행률 판단에 필요한 최소 요약입니다.
/// </summary>
/// <param name="ProductionOrderId">진행률을 계산한 생산오더 식별자입니다.</param>
/// <param name="TotalOperationCount">오더에 연결된 전체 공정 수입니다.</param>
/// <param name="RemainingOpenOperationCountExcludingCurrent">현재 완료 대상 공정을 제외하고 아직 완료되지 않은 공정 수입니다.</param>
/// <param name="CompletedOperationCountIncludingCurrentAfterAccept">현재 완료를 받아들인 뒤 완료 상태로 간주되는 총 공정 수입니다.</param>
public sealed record OrderCompletionProgressSnapshot(
    string ProductionOrderId,
    int TotalOperationCount,
    int RemainingOpenOperationCountExcludingCurrent,
    int CompletedOperationCountIncludingCurrentAfterAccept);

/// <summary>
/// 공정 완료 처리에 필요한 aggregate 상태 묶음입니다.
/// </summary>
/// <param name="ProductionOrder">상위 생산오더입니다.</param>
/// <param name="OperationExecution">완료 대상 공정 실행입니다.</param>
/// <param name="OrderCompletionProgress">생산오더 완료 진행률 판단에 필요한 sibling-operation 요약입니다.</param>
public sealed record CompleteOperationCommandState(
    ProductionOrder ProductionOrder,
    OperationExecution OperationExecution,
    OrderCompletionProgressSnapshot OrderCompletionProgress);

/// <summary>
/// 작업 큐 query handler가 받는 입력 묶음입니다.
/// </summary>
/// <param name="Request">작업 큐 요청 계약입니다.</param>
/// <param name="Source">작업 큐를 구성할 MES-side source입니다.</param>
/// <param name="SnapshotTakenAt">snapshot 생성 시각입니다.</param>
public sealed record GetStationWorkQueueHandlingRequest(
    GetStationWorkQueueRequestContract Request,
    WorkQueue.StationWorkQueueSource Source,
    DateTimeOffset SnapshotTakenAt);
