using Mes.Application.Idempotency;
using Mes.Application.OperatorExecution;
using Mes.Application.OperatorExecution.WorkQueue;
using Mes.Domain.Abstractions;
using Mes.Domain.Aggregates;
using Mes.Domain.Entities;

namespace Mes.Infrastructure.OperatorExecution.Sqlite;

/// <summary>
/// SQLite 저장소 초기 적재에 사용하는 operator-execution 시드 묶음입니다.
/// </summary>
public sealed record SqliteOperatorExecutionSeed
{
    /// <summary>
    /// 초기 생산 오더 목록입니다.
    /// </summary>
    public IReadOnlyCollection<ProductionOrder> ProductionOrders { get; init; } = [];

    /// <summary>
    /// 초기 공정 실행 목록입니다.
    /// </summary>
    public IReadOnlyCollection<OperationExecution> OperationExecutions { get; init; } = [];

    /// <summary>
    /// 초기 WIP 목록입니다.
    /// </summary>
    public IReadOnlyCollection<WipUnit> WipUnits { get; init; } = [];

    /// <summary>
    /// 초기 자재 lot 목록입니다.
    /// </summary>
    public IReadOnlyCollection<MaterialLot> MaterialLots { get; init; } = [];

    /// <summary>
    /// 초기 품질 기록 목록입니다.
    /// </summary>
    public IReadOnlyCollection<QualityRecord> QualityRecords { get; init; } = [];

    /// <summary>
    /// 초기 자재 요구 snapshot 목록입니다.
    /// </summary>
    public IReadOnlyCollection<OperationMaterialRequirementSnapshot> MaterialRequirements { get; init; } = [];

    /// <summary>
    /// 초기 command receipt 목록입니다.
    /// </summary>
    public IReadOnlyCollection<CommandReceiptRecord> Receipts { get; init; } = [];

    /// <summary>
    /// 초기 production actuals batch 목록입니다.
    /// </summary>
    public IReadOnlyCollection<PreparedProductionActualsBatch> ProductionActualsBatches { get; init; } = [];
}

/// <summary>
/// SQLite 저장소 커밋 시 전달하는 변경 묶음입니다.
/// </summary>
internal sealed record SqliteOperatorExecutionCommitRequest
{
    /// <summary>
    /// 갱신할 생산 오더 목록입니다.
    /// </summary>
    public IReadOnlyCollection<ProductionOrder> ProductionOrders { get; init; } = [];

    /// <summary>
    /// 갱신할 공정 실행 목록입니다.
    /// </summary>
    public IReadOnlyCollection<OperationExecution> OperationExecutions { get; init; } = [];

    /// <summary>
    /// 갱신할 WIP 목록입니다.
    /// </summary>
    public IReadOnlyCollection<WipUnit> WipUnits { get; init; } = [];

    /// <summary>
    /// 갱신할 자재 lot 목록입니다.
    /// </summary>
    public IReadOnlyCollection<MaterialLot> MaterialLots { get; init; } = [];

    /// <summary>
    /// 갱신할 품질 기록 목록입니다.
    /// </summary>
    public IReadOnlyCollection<QualityRecord> QualityRecords { get; init; } = [];

    /// <summary>
    /// 저장할 command receipt입니다.
    /// </summary>
    public CommandReceiptRecord? Receipt { get; init; }

    /// <summary>
    /// 저장할 production actuals batch입니다.
    /// </summary>
    public PreparedProductionActualsBatch? PreparedBatch { get; init; }

    /// <summary>
    /// 저장할 outbox 초안 목록입니다.
    /// </summary>
    public IReadOnlyCollection<SqliteOperatorExecutionOutboxDraft> OutboxEntries { get; init; } = [];

    /// <summary>
    /// 커밋 기준 시각입니다.
    /// </summary>
    public DateTimeOffset CommittedAt { get; init; }
}

/// <summary>
/// SQLite 저장소에 적재할 outbox 초안입니다.
/// </summary>
/// <param name="AggregateType">이벤트를 발생시킨 aggregate 형식입니다.</param>
/// <param name="AggregateId">이벤트를 발생시킨 aggregate 식별자입니다.</param>
/// <param name="DomainEvent">저장할 domain event입니다.</param>
internal sealed record SqliteOperatorExecutionOutboxDraft(
    string AggregateType,
    string AggregateId,
    IDomainEvent DomainEvent);

/// <summary>
/// SQLite 저장소에서 inspection 용도로 읽는 outbox 항목입니다.
/// </summary>
/// <param name="OutboxEventId">outbox 이벤트 식별자입니다.</param>
/// <param name="AggregateType">원본 aggregate 형식입니다.</param>
/// <param name="AggregateId">원본 aggregate 식별자입니다.</param>
/// <param name="EventType">domain event 형식 이름입니다.</param>
/// <param name="OccurredAt">domain event 발생 시각입니다.</param>
/// <param name="PayloadJson">저장된 event payload JSON입니다.</param>
/// <param name="PersistedAt">outbox 저장 시각입니다.</param>
public sealed record SqliteOperatorExecutionOutboxEntry(
    string OutboxEventId,
    string AggregateType,
    string AggregateId,
    string EventType,
    DateTimeOffset OccurredAt,
    string PayloadJson,
    DateTimeOffset PersistedAt);
