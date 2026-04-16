using Mes.Application.Idempotency;
using Mes.Application.OperatorExecution;
using Mes.Application.OperatorExecution.WorkQueue;
using Mes.Domain.Abstractions;
using Mes.Domain.Aggregates;
using Mes.Domain.Entities;
using Mes.Domain.ValueObjects;

namespace Mes.Infrastructure.OperatorExecution.InMemory;

/// <summary>
/// 운영자 실행 슬라이스를 위한 in-memory 기준 저장소를 제공합니다.
/// </summary>
public sealed class InMemoryOperatorExecutionStore
{
    private readonly object _gate = new();
    private readonly Dictionary<string, ProductionOrder> _productionOrders = new(StringComparer.Ordinal);
    private readonly Dictionary<string, OperationExecution> _operationExecutions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, WipUnit> _wipUnits = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MaterialLot> _materialLots = new(StringComparer.Ordinal);
    private readonly Dictionary<string, QualityRecord> _qualityRecords = new(StringComparer.Ordinal);
    private readonly Dictionary<string, OperationMaterialRequirementSnapshot> _materialRequirements = new(StringComparer.Ordinal);
    private readonly Dictionary<InMemoryCommandReceiptKey, CommandReceiptRecord> _receipts = [];
    private readonly Dictionary<string, PreparedProductionActualsBatch> _productionActualsBatches = new(StringComparer.Ordinal);
    private readonly List<InMemoryOutboxEntry> _outboxEntries = [];
    private long _outboxSequence;

    /// <summary>
    /// 외부에서 inspection할 수 있는 command receipt 스냅샷을 반환합니다.
    /// </summary>
    public IReadOnlyList<CommandReceiptRecord> Receipts
    {
        get
        {
            lock (_gate)
            {
                return _receipts.Values.ToList();
            }
        }
    }

    /// <summary>
    /// 외부에서 inspection할 수 있는 production actuals batch 스냅샷을 반환합니다.
    /// </summary>
    public IReadOnlyList<PreparedProductionActualsBatch> ProductionActualsBatches
    {
        get
        {
            lock (_gate)
            {
                return _productionActualsBatches.Values.ToList();
            }
        }
    }

    /// <summary>
    /// 외부에서 inspection할 수 있는 outbox entry 스냅샷을 반환합니다.
    /// </summary>
    public IReadOnlyList<InMemoryOutboxEntry> OutboxEntries
    {
        get
        {
            lock (_gate)
            {
                return _outboxEntries.ToList();
            }
        }
    }

    /// <summary>
    /// 테스트나 bootstrap 시드로 사용할 초기 데이터를 저장소에 적재합니다.
    /// </summary>
    /// <param name="seed">적재할 초기 데이터 묶음입니다.</param>
    public void Seed(InMemoryOperatorExecutionSeed seed)
    {
        ArgumentNullException.ThrowIfNull(seed);

        lock (_gate)
        {
            SeedAggregates(_productionOrders, seed.ProductionOrders);
            SeedAggregates(_operationExecutions, seed.OperationExecutions);
            SeedEntities(_wipUnits, seed.WipUnits, wipUnit => wipUnit.Id.ToString());
            SeedAggregates(_materialLots, seed.MaterialLots);
            SeedAggregates(_qualityRecords, seed.QualityRecords);

            foreach (var requirement in seed.MaterialRequirements)
            {
                _materialRequirements[requirement.OperationMaterialRequirementId] = requirement;
            }

            foreach (var receipt in seed.Receipts)
            {
                _receipts[InMemoryCommandReceiptKey.From(receipt.Scope)] = receipt;
            }

            foreach (var batch in seed.ProductionActualsBatches)
            {
                _productionActualsBatches[batch.ActualsBatchId] = batch;
            }
        }
    }

    /// <summary>
    /// 자연 유일 scope 기준으로 기존 receipt를 조회합니다.
    /// </summary>
    /// <param name="scope">조회할 receipt scope입니다.</param>
    /// <returns>기존 receipt가 있으면 반환합니다.</returns>
    public CommandReceiptRecord? FindReceipt(CommandReceiptScope scope)
    {
        lock (_gate)
        {
            return _receipts.TryGetValue(InMemoryCommandReceiptKey.From(scope), out var receipt)
                ? receipt
                : null;
        }
    }

    /// <summary>
    /// 생산오더를 조회합니다.
    /// </summary>
    /// <param name="productionOrderId">생산오더 식별자입니다.</param>
    /// <returns>저장된 생산오더 aggregate입니다.</returns>
    public ProductionOrder GetProductionOrder(string productionOrderId)
    {
        lock (_gate)
        {
            return Require(_productionOrders, productionOrderId, nameof(productionOrderId));
        }
    }

    /// <summary>
    /// 공정 실행을 조회합니다.
    /// </summary>
    /// <param name="operationExecutionId">공정 실행 식별자입니다.</param>
    /// <returns>저장된 공정 실행 aggregate입니다.</returns>
    public OperationExecution GetOperationExecution(string operationExecutionId)
    {
        lock (_gate)
        {
            return Require(_operationExecutions, operationExecutionId, nameof(operationExecutionId));
        }
    }

    /// <summary>
    /// WIP 단위를 조회합니다.
    /// </summary>
    /// <param name="wipUnitId">WIP 식별자입니다.</param>
    /// <returns>저장된 WIP 엔티티입니다.</returns>
    public WipUnit GetWipUnit(string wipUnitId)
    {
        lock (_gate)
        {
            return Require(_wipUnits, wipUnitId, nameof(wipUnitId));
        }
    }

    /// <summary>
    /// 자재 lot을 조회합니다.
    /// </summary>
    /// <param name="materialLotId">자재 lot 식별자입니다.</param>
    /// <returns>저장된 자재 lot aggregate입니다.</returns>
    public MaterialLot GetMaterialLot(string materialLotId)
    {
        lock (_gate)
        {
            return Require(_materialLots, materialLotId, nameof(materialLotId));
        }
    }

    /// <summary>
    /// 품질 기록을 조회합니다.
    /// </summary>
    /// <param name="qualityRecordId">품질 기록 식별자입니다.</param>
    /// <returns>저장된 품질 기록 aggregate입니다.</returns>
    public QualityRecord GetQualityRecord(string qualityRecordId)
    {
        lock (_gate)
        {
            return Require(_qualityRecords, qualityRecordId, nameof(qualityRecordId));
        }
    }

    /// <summary>
    /// 작업 큐 구성을 위해 현재 저장소 snapshot을 반환합니다.
    /// </summary>
    /// <returns>MES-side source snapshot입니다.</returns>
    public StationWorkQueueSource BuildWorkQueueSource()
    {
        lock (_gate)
        {
            return new StationWorkQueueSource(
                _operationExecutions.Values.ToList(),
                _wipUnits.Values.ToList(),
                _qualityRecords.Values.ToList(),
                _materialRequirements.Values
                    .OrderBy(requirement => requirement.Metadata.SequenceNo)
                    .ToList());
        }
    }

    /// <summary>
    /// receipt, actuals batch, outbox entry를 하나의 저장 경계로 커밋합니다.
    /// </summary>
    /// <param name="writeSet">커밋할 저장 묶음입니다.</param>
    public void Commit(InMemoryOperatorExecutionWriteSet writeSet)
    {
        ArgumentNullException.ThrowIfNull(writeSet);

        lock (_gate)
        {
            if (writeSet.Receipt is not null)
            {
                _receipts[InMemoryCommandReceiptKey.From(writeSet.Receipt.Scope)] = writeSet.Receipt;
            }

            if (writeSet.PreparedBatch is not null)
            {
                _productionActualsBatches[writeSet.PreparedBatch.ActualsBatchId] = writeSet.PreparedBatch;
            }

            foreach (var draft in writeSet.OutboxEntries)
            {
                _outboxEntries.Add(new InMemoryOutboxEntry(
                    CreateOutboxEventId(),
                    draft.AggregateType,
                    draft.AggregateId,
                    draft.DomainEvent.GetType().Name,
                    draft.DomainEvent.OccurredAt,
                    draft.DomainEvent,
                    writeSet.CommittedAt));
            }
        }
    }

    /// <summary>
    /// aggregate 시드를 저장소에 적재하고 기존 domain event를 비웁니다.
    /// </summary>
    /// <typeparam name="TAggregate">aggregate 형식입니다.</typeparam>
    /// <typeparam name="TKey">사전 키 형식입니다.</typeparam>
    /// <param name="target">적재 대상 사전입니다.</param>
    /// <param name="aggregates">적재할 aggregate 목록입니다.</param>
    private static void SeedAggregates<TAggregate, TKey>(
        IDictionary<TKey, TAggregate> target,
        IEnumerable<TAggregate> aggregates)
        where TAggregate : class
        where TKey : notnull
    {
        foreach (var aggregate in aggregates)
        {
            ClearDomainEvents(aggregate);
            target[GetAggregateKey<TKey>(aggregate)] = aggregate;
        }
    }

    /// <summary>
    /// entity 시드를 저장소에 적재합니다.
    /// </summary>
    /// <typeparam name="TEntity">entity 형식입니다.</typeparam>
    /// <typeparam name="TKey">사전 키 형식입니다.</typeparam>
    /// <param name="target">적재 대상 사전입니다.</param>
    /// <param name="entities">적재할 entity 목록입니다.</param>
    /// <param name="getKey">entity 키 선택기입니다.</param>
    private static void SeedEntities<TEntity, TKey>(
        IDictionary<TKey, TEntity> target,
        IEnumerable<TEntity> entities,
        Func<TEntity, TKey> getKey)
        where TEntity : class
        where TKey : notnull
    {
        foreach (var entity in entities)
        {
            target[getKey(entity)] = entity;
        }
    }

    /// <summary>
    /// aggregate나 entity를 키 기준으로 조회합니다.
    /// </summary>
    /// <typeparam name="TValue">값 형식입니다.</typeparam>
    /// <param name="source">조회할 사전입니다.</param>
    /// <param name="key">조회 키입니다.</param>
    /// <param name="parameterName">예외 메시지에 사용할 파라미터 이름입니다.</param>
    /// <returns>저장된 값입니다.</returns>
    private static TValue Require<TValue>(
        IReadOnlyDictionary<string, TValue> source,
        string key,
        string parameterName)
        where TValue : class
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Required string value cannot be empty.", parameterName);
        }

        if (source.TryGetValue(key.Trim(), out var value))
        {
            return value;
        }

        throw new InvalidOperationException($"No stored value exists for {parameterName} '{key}'.");
    }

    /// <summary>
    /// aggregate에서 공통 문자열 키를 추출합니다.
    /// </summary>
    /// <typeparam name="TKey">반환할 키 형식입니다.</typeparam>
    /// <param name="aggregate">키를 추출할 aggregate입니다.</param>
    /// <returns>aggregate 식별자 문자열입니다.</returns>
    private static TKey GetAggregateKey<TKey>(object aggregate)
        where TKey : notnull
    {
        var key = aggregate switch
        {
            ProductionOrder order => order.Id.ToString(),
            OperationExecution operationExecution => operationExecution.Id.ToString(),
            MaterialLot materialLot => materialLot.Id.ToString(),
            QualityRecord qualityRecord => qualityRecord.Id.ToString(),
            _ => throw new InvalidOperationException($"Unsupported aggregate seed type: {aggregate.GetType().Name}.")
        };

        return (TKey)(object)key;
    }

    /// <summary>
    /// 지원하는 aggregate의 domain event를 비웁니다.
    /// </summary>
    /// <param name="aggregate">event를 비울 aggregate입니다.</param>
    private static void ClearDomainEvents(object aggregate)
    {
        switch (aggregate)
        {
            case ProductionOrder order:
                order.ClearDomainEvents();
                break;
            case OperationExecution operationExecution:
                operationExecution.ClearDomainEvents();
                break;
            case MaterialLot materialLot:
                materialLot.ClearDomainEvents();
                break;
            case QualityRecord qualityRecord:
                qualityRecord.ClearDomainEvents();
                break;
            default:
                throw new InvalidOperationException($"Unsupported aggregate type for event clearing: {aggregate.GetType().Name}.");
        }
    }

    /// <summary>
    /// outbox entry 식별자를 생성합니다.
    /// </summary>
    /// <returns>저장소 내에서 유일한 outbox 식별자입니다.</returns>
    private string CreateOutboxEventId()
    {
        _outboxSequence++;
        return $"OUT-{_outboxSequence:000000}";
    }
}

/// <summary>
/// in-memory 저장소 초기 데이터 시드입니다.
/// </summary>
public sealed record InMemoryOperatorExecutionSeed
{
    /// <summary>
    /// 생산오더 목록을 비어 있지 않은 컬렉션으로 반환합니다.
    /// </summary>
    public IReadOnlyCollection<ProductionOrder> ProductionOrders { get; init; } = [];

    /// <summary>
    /// 공정 실행 목록을 비어 있지 않은 컬렉션으로 반환합니다.
    /// </summary>
    public IReadOnlyCollection<OperationExecution> OperationExecutions { get; init; } = [];

    /// <summary>
    /// WIP 목록을 비어 있지 않은 컬렉션으로 반환합니다.
    /// </summary>
    public IReadOnlyCollection<WipUnit> WipUnits { get; init; } = [];

    /// <summary>
    /// 자재 lot 목록을 비어 있지 않은 컬렉션으로 반환합니다.
    /// </summary>
    public IReadOnlyCollection<MaterialLot> MaterialLots { get; init; } = [];

    /// <summary>
    /// 품질 기록 목록을 비어 있지 않은 컬렉션으로 반환합니다.
    /// </summary>
    public IReadOnlyCollection<QualityRecord> QualityRecords { get; init; } = [];

    /// <summary>
    /// 자재 요구 snapshot 목록을 비어 있지 않은 컬렉션으로 반환합니다.
    /// </summary>
    public IReadOnlyCollection<OperationMaterialRequirementSnapshot> MaterialRequirements { get; init; } = [];

    /// <summary>
    /// 기존 receipt 목록을 비어 있지 않은 컬렉션으로 반환합니다.
    /// </summary>
    public IReadOnlyCollection<CommandReceiptRecord> Receipts { get; init; } = [];

    /// <summary>
    /// 기존 actuals batch 목록을 비어 있지 않은 컬렉션으로 반환합니다.
    /// </summary>
    public IReadOnlyCollection<PreparedProductionActualsBatch> ProductionActualsBatches { get; init; } = [];
}

/// <summary>
/// in-memory 저장소의 원자적 저장 묶음을 표현합니다.
/// </summary>
/// <param name="Receipt">저장할 receipt입니다.</param>
/// <param name="PreparedBatch">저장할 production actuals batch입니다.</param>
/// <param name="OutboxEntries">함께 저장할 outbox entry 초안입니다.</param>
/// <param name="CommittedAt">저장 시각입니다.</param>
public sealed record InMemoryOperatorExecutionWriteSet(
    CommandReceiptRecord? Receipt,
    PreparedProductionActualsBatch? PreparedBatch,
    IReadOnlyCollection<InMemoryOutboxEntryDraft> OutboxEntries,
    DateTimeOffset CommittedAt);

/// <summary>
/// in-memory outbox entry 초안을 표현합니다.
/// </summary>
/// <param name="AggregateType">원본 aggregate 형식입니다.</param>
/// <param name="AggregateId">원본 aggregate 식별자입니다.</param>
/// <param name="DomainEvent">저장할 domain event입니다.</param>
public sealed record InMemoryOutboxEntryDraft(
    string AggregateType,
    string AggregateId,
    IDomainEvent DomainEvent);

/// <summary>
/// in-memory outbox entry 저장 결과를 표현합니다.
/// </summary>
/// <param name="OutboxEventId">outbox 식별자입니다.</param>
/// <param name="AggregateType">원본 aggregate 형식입니다.</param>
/// <param name="AggregateId">원본 aggregate 식별자입니다.</param>
/// <param name="EventType">domain event 형식 이름입니다.</param>
/// <param name="OccurredAt">domain event 발생 시각입니다.</param>
/// <param name="DomainEvent">원본 domain event입니다.</param>
/// <param name="PersistedAt">저장 시각입니다.</param>
public sealed record InMemoryOutboxEntry(
    string OutboxEventId,
    string AggregateType,
    string AggregateId,
    string EventType,
    DateTimeOffset OccurredAt,
    IDomainEvent DomainEvent,
    DateTimeOffset PersistedAt);

/// <summary>
/// in-memory receipt 조회용 자연 키를 표현합니다.
/// </summary>
/// <param name="Channel">채널 값입니다.</param>
/// <param name="CommandType">command 형식입니다.</param>
/// <param name="IdempotencyKey">idempotency 키입니다.</param>
internal sealed record InMemoryCommandReceiptKey(
    string Channel,
    string CommandType,
    string IdempotencyKey)
{
    /// <summary>
    /// receipt scope에서 in-memory 자연 키를 생성합니다.
    /// </summary>
    /// <param name="scope">receipt scope입니다.</param>
    /// <returns>정규화된 자연 키입니다.</returns>
    public static InMemoryCommandReceiptKey From(CommandReceiptScope scope)
    {
        return new InMemoryCommandReceiptKey(
            scope.Channel.Trim().ToLowerInvariant(),
            scope.CommandType.Trim(),
            scope.IdempotencyKey.Trim());
    }
}
