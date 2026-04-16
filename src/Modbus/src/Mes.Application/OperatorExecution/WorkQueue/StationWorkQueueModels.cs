using Mes.Domain.Aggregates;
using Mes.Domain.Entities;
using Mes.Domain.Statuses;
using Mes.Domain.ValueObjects;

namespace Mes.Application.OperatorExecution.WorkQueue;

/// <summary>
/// 작업 큐 품질 게이트 상태를 표현합니다.
/// </summary>
public enum StationWorkQueueQualityGateState
{
    Open,
    Hold
}

/// <summary>
/// 작업 큐 조회에 필요한 MES-side source 묶음입니다.
/// </summary>
/// <param name="OperationExecutions">조회 후보 공정 실행 목록입니다.</param>
/// <param name="WipUnits">공정 실행과 연결된 WIP 목록입니다.</param>
/// <param name="QualityRecords">WIP에 연결된 품질 기록 목록입니다.</param>
/// <param name="MaterialRequirements">공정 실행별 자재 요구량 snapshot 목록입니다.</param>
public sealed record StationWorkQueueSource(
    IReadOnlyCollection<OperationExecution> OperationExecutions,
    IReadOnlyCollection<WipUnit> WipUnits,
    IReadOnlyCollection<QualityRecord> QualityRecords,
    IReadOnlyCollection<OperationMaterialRequirementSnapshot> MaterialRequirements);

/// <summary>
/// 작업 큐 조회 요청입니다.
/// </summary>
/// <param name="StationId">조회 대상 스테이션 식별자입니다.</param>
/// <param name="SnapshotTakenAt">조회 시각입니다.</param>
/// <param name="Source">조회에 사용할 MES-side source입니다.</param>
public sealed record GetStationWorkQueueQueryRequest(
    StationId StationId,
    DateTimeOffset SnapshotTakenAt,
    StationWorkQueueSource Source);

/// <summary>
/// 작업 큐 항목의 실행 식별 정보입니다.
/// </summary>
/// <param name="ProductionOrderId">생산 오더 식별자입니다.</param>
/// <param name="OperationExecutionId">공정 실행 식별자입니다.</param>
/// <param name="OperationSequence">공정 순번입니다.</param>
/// <param name="StationId">배정된 스테이션 식별자입니다.</param>
public sealed record StationWorkQueueIdentity(
    ProductionOrderId ProductionOrderId,
    OperationExecutionId OperationExecutionId,
    int OperationSequence,
    StationId StationId);

/// <summary>
/// 작업 큐 항목의 상태 요약입니다.
/// </summary>
/// <param name="OperationStatus">현재 공정 실행 상태입니다.</param>
/// <param name="QualityGateState">현재 품질 게이트 상태입니다.</param>
public sealed record StationWorkQueueState(
    OperationExecutionStatus OperationStatus,
    StationWorkQueueQualityGateState QualityGateState);

/// <summary>
/// 작업 큐에서 표시할 자재 요구량 항목입니다.
/// </summary>
/// <param name="MaterialCode">자재 코드입니다.</param>
/// <param name="RequiredQuantity">요구 수량입니다.</param>
/// <param name="SequenceNo">표시 순서입니다.</param>
public sealed record StationWorkQueueRequiredMaterial(
    string MaterialCode,
    MeasuredQuantity RequiredQuantity,
    int SequenceNo);

/// <summary>
/// 작업 큐의 단일 공정 항목입니다.
/// </summary>
/// <param name="Identity">공정 식별 정보입니다.</param>
/// <param name="State">공정 및 품질 게이트 상태입니다.</param>
/// <param name="RequiredMaterials">MES-side snapshot에서 읽은 자재 요구량 목록입니다.</param>
public sealed record StationWorkQueueItem(
    StationWorkQueueIdentity Identity,
    StationWorkQueueState State,
    IReadOnlyList<StationWorkQueueRequiredMaterial> RequiredMaterials);

/// <summary>
/// 스테이션 작업 큐 snapshot입니다.
/// </summary>
/// <param name="StationId">조회 대상 스테이션 식별자입니다.</param>
/// <param name="SnapshotTakenAt">snapshot 시각입니다.</param>
/// <param name="Items">작업 큐 항목 목록입니다.</param>
public sealed record StationWorkQueueSnapshot(
    StationId StationId,
    DateTimeOffset SnapshotTakenAt,
    IReadOnlyList<StationWorkQueueItem> Items);
