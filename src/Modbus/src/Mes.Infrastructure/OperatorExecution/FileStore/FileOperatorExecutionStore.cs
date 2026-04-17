using System.Text.Json;
using System.Text.Json.Serialization;
using Mes.Application.Idempotency;
using Mes.Application.OperatorExecution;
using Mes.Application.OperatorExecution.WorkQueue;
using Mes.Domain.Aggregates;
using Mes.Domain.Entities;
using Mes.Domain.Statuses;

namespace Mes.Infrastructure.OperatorExecution.FileStore;

/// <summary>
/// operator-execution 상태를 단일 JSON 스냅샷 파일에 저장하는 durable 저장소입니다.
/// </summary>
public sealed class FileOperatorExecutionStore
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private readonly object _gate = new();
    private readonly string _storeFilePath;
    private FileOperatorExecutionPersistenceState _state;

    /// <summary>
    /// 파일 저장소를 초기화하고 기존 스냅샷을 로드합니다.
    /// </summary>
    /// <param name="options">저장소 파일 경로 옵션입니다.</param>
    public FileOperatorExecutionStore(FileOperatorExecutionStoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.StoreFilePath))
        {
            throw new ArgumentException("Store file path is required.", nameof(options));
        }

        _storeFilePath = Path.GetFullPath(options.StoreFilePath);
        EnsureParentDirectoryExists(_storeFilePath);
        _state = LoadState(_storeFilePath);
    }

    /// <summary>
    /// inspection용 receipt 목록입니다.
    /// </summary>
    public IReadOnlyList<CommandReceiptRecord> Receipts
    {
        get
        {
            lock (_gate)
            {
                return _state.Receipts.Values.ToList();
            }
        }
    }

    /// <summary>
    /// inspection용 production actuals batch 목록입니다.
    /// </summary>
    public IReadOnlyList<PreparedProductionActualsBatch> ProductionActualsBatches
    {
        get
        {
            lock (_gate)
            {
                return _state.ProductionActualsBatches.Values.ToList();
            }
        }
    }

    /// <summary>
    /// inspection용 outbox 목록입니다.
    /// </summary>
    public IReadOnlyList<FileOperatorExecutionOutboxEntry> OutboxEntries
    {
        get
        {
            lock (_gate)
            {
                return _state.OutboxEntries.ToList();
            }
        }
    }

    /// <summary>
    /// 테스트나 부트스트랩용 초기 데이터를 저장소에 적재합니다.
    /// </summary>
    /// <param name="seed">적재할 초기 데이터입니다.</param>
    public void Seed(FileOperatorExecutionSeed seed)
    {
        ArgumentNullException.ThrowIfNull(seed);

        lock (_gate)
        {
            var nextState = new FileOperatorExecutionPersistenceState();

            foreach (var order in seed.ProductionOrders)
            {
                nextState.ProductionOrders[order.Id.ToString()] = ProductionOrderFileSnapshot.FromAggregate(order);
            }

            foreach (var operation in seed.OperationExecutions)
            {
                nextState.OperationExecutions[operation.Id.ToString()] = OperationExecutionFileSnapshot.FromAggregate(operation);
            }

            foreach (var wipUnit in seed.WipUnits)
            {
                nextState.WipUnits[wipUnit.Id.ToString()] = WipUnitFileSnapshot.FromEntity(wipUnit);
            }

            foreach (var materialLot in seed.MaterialLots)
            {
                nextState.MaterialLots[materialLot.Id.ToString()] = MaterialLotFileSnapshot.FromAggregate(materialLot);
            }

            foreach (var qualityRecord in seed.QualityRecords)
            {
                nextState.QualityRecords[qualityRecord.Id.ToString()] = QualityRecordFileSnapshot.FromAggregate(qualityRecord);
            }

            foreach (var requirement in seed.MaterialRequirements)
            {
                nextState.MaterialRequirements[requirement.OperationMaterialRequirementId] = OperationMaterialRequirementFileSnapshot.FromSnapshot(requirement);
            }

            foreach (var receipt in seed.Receipts)
            {
                nextState.Receipts[CreateReceiptKey(receipt.Scope)] = receipt;
            }

            foreach (var batch in seed.ProductionActualsBatches)
            {
                nextState.ProductionActualsBatches[batch.ActualsBatchId] = batch;
            }

            PersistAndSwap(nextState);
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
            return _state.Receipts.TryGetValue(CreateReceiptKey(scope), out var receipt)
                ? receipt
                : null;
        }
    }

    /// <summary>
    /// 생산 오더 aggregate를 복원해 조회합니다.
    /// </summary>
    /// <param name="productionOrderId">생산 오더 식별자입니다.</param>
    /// <returns>복원된 생산 오더 aggregate입니다.</returns>
    public ProductionOrder GetProductionOrder(string productionOrderId)
    {
        lock (_gate)
        {
            return Require(_state.ProductionOrders, productionOrderId, nameof(productionOrderId)).Restore();
        }
    }

    /// <summary>
    /// 공정 실행 aggregate를 복원해 조회합니다.
    /// </summary>
    /// <param name="operationExecutionId">공정 실행 식별자입니다.</param>
    /// <returns>복원된 공정 실행 aggregate입니다.</returns>
    public OperationExecution GetOperationExecution(string operationExecutionId)
    {
        lock (_gate)
        {
            return Require(_state.OperationExecutions, operationExecutionId, nameof(operationExecutionId)).Restore();
        }
    }

    /// <summary>
    /// 생산오더 완료 진행률 판단에 필요한 sibling-operation 요약을 계산합니다.
    /// </summary>
    /// <param name="productionOrderId">요약을 계산할 생산오더 식별자입니다.</param>
    /// <param name="currentOperationExecutionId">현재 완료 처리 중인 공정 실행 식별자입니다.</param>
    /// <returns>완료 진행률 판단에 필요한 최소 요약입니다.</returns>
    public OrderCompletionProgressSnapshot GetOrderCompletionProgress(string productionOrderId, string currentOperationExecutionId)
    {
        lock (_gate)
        {
            var order = Require(_state.ProductionOrders, productionOrderId, nameof(productionOrderId));
            var normalizedCurrentOperationExecutionId = NormalizeRequired(currentOperationExecutionId, nameof(currentOperationExecutionId));
            var totalOperationCount = order.OperationIds.Count;
            var remainingOpenOperationCount = order.OperationIds
                .Where(operationExecutionId => !string.Equals(operationExecutionId, normalizedCurrentOperationExecutionId, StringComparison.Ordinal))
                .Select(operationExecutionId => Require(_state.OperationExecutions, operationExecutionId, nameof(currentOperationExecutionId)))
                .Count(operation => !IsCompletedForOrderProgression(operation.Status));

            return new OrderCompletionProgressSnapshot(
                order.ProductionOrderId,
                totalOperationCount,
                remainingOpenOperationCount,
                totalOperationCount - remainingOpenOperationCount);
        }
    }

    /// <summary>
    /// WIP 엔티티를 복원해 조회합니다.
    /// </summary>
    /// <param name="wipUnitId">WIP 식별자입니다.</param>
    /// <returns>복원된 WIP 엔티티입니다.</returns>
    public WipUnit GetWipUnit(string wipUnitId)
    {
        lock (_gate)
        {
            return Require(_state.WipUnits, wipUnitId, nameof(wipUnitId)).Restore();
        }
    }

    /// <summary>
    /// 자재 lot aggregate를 복원해 조회합니다.
    /// </summary>
    /// <param name="materialLotId">자재 lot 식별자입니다.</param>
    /// <returns>복원된 자재 lot aggregate입니다.</returns>
    public MaterialLot GetMaterialLot(string materialLotId)
    {
        lock (_gate)
        {
            return Require(_state.MaterialLots, materialLotId, nameof(materialLotId)).Restore();
        }
    }

    /// <summary>
    /// 지정한 공정 실행에 연결된 요구 자재 snapshot을 조회합니다.
    /// </summary>
    /// <param name="operationExecutionId">대상 공정 실행 식별자입니다.</param>
    /// <returns>공정 실행 기준 요구 자재 snapshot 목록입니다.</returns>
    public IReadOnlyCollection<OperationMaterialRequirementSnapshot> GetMaterialRequirements(string operationExecutionId)
    {
        lock (_gate)
        {
            return _state.MaterialRequirements.Values
                .Select(snapshot => snapshot.Restore())
                .Where(requirement => string.Equals(requirement.OperationExecutionId.ToString(), operationExecutionId, StringComparison.Ordinal))
                .OrderBy(requirement => requirement.Metadata.SequenceNo)
                .ToList();
        }
    }

    /// <summary>
    /// 품질 기록 aggregate를 복원해 조회합니다.
    /// </summary>
    /// <param name="qualityRecordId">품질 기록 식별자입니다.</param>
    /// <returns>복원된 품질 기록 aggregate입니다.</returns>
    public QualityRecord GetQualityRecord(string qualityRecordId)
    {
        lock (_gate)
        {
            return Require(_state.QualityRecords, qualityRecordId, nameof(qualityRecordId)).Restore();
        }
    }

    /// <summary>
    /// 현재 저장소 스냅샷에서 station work queue source를 구성합니다.
    /// </summary>
    /// <returns>MES-side work queue source입니다.</returns>
    public StationWorkQueueSource BuildWorkQueueSource()
    {
        lock (_gate)
        {
            return new StationWorkQueueSource(
                _state.OperationExecutions.Values.Select(snapshot => snapshot.Restore()).ToList(),
                _state.WipUnits.Values.Select(snapshot => snapshot.Restore()).ToList(),
                _state.QualityRecords.Values.Select(snapshot => snapshot.Restore()).ToList(),
                _state.MaterialRequirements.Values
                    .Select(snapshot => snapshot.Restore())
                    .OrderBy(requirement => requirement.Metadata.SequenceNo)
                    .ToList());
        }
    }

    /// <summary>
    /// aggregate 스냅샷, receipt, actuals batch, outbox를 하나의 파일 교체 경계로 커밋합니다.
    /// </summary>
    /// <param name="request">커밋할 변경 묶음입니다.</param>
    internal void Commit(FileOperatorExecutionCommitRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (_gate)
        {
            var nextState = CloneState(_state);

            foreach (var order in request.ProductionOrders)
            {
                nextState.ProductionOrders[order.Id.ToString()] = ProductionOrderFileSnapshot.FromAggregate(order);
            }

            foreach (var operation in request.OperationExecutions)
            {
                nextState.OperationExecutions[operation.Id.ToString()] = OperationExecutionFileSnapshot.FromAggregate(operation);
            }

            foreach (var wipUnit in request.WipUnits)
            {
                nextState.WipUnits[wipUnit.Id.ToString()] = WipUnitFileSnapshot.FromEntity(wipUnit);
            }

            foreach (var materialLot in request.MaterialLots)
            {
                nextState.MaterialLots[materialLot.Id.ToString()] = MaterialLotFileSnapshot.FromAggregate(materialLot);
            }

            foreach (var qualityRecord in request.QualityRecords)
            {
                nextState.QualityRecords[qualityRecord.Id.ToString()] = QualityRecordFileSnapshot.FromAggregate(qualityRecord);
            }

            if (request.Receipt is not null)
            {
                nextState.Receipts[CreateReceiptKey(request.Receipt.Scope)] = request.Receipt;
            }

            if (request.PreparedBatch is not null)
            {
                nextState.ProductionActualsBatches[request.PreparedBatch.ActualsBatchId] = request.PreparedBatch;
            }

            foreach (var outboxDraft in request.OutboxEntries)
            {
                nextState.OutboxSequence++;
                nextState.OutboxEntries.Add(new FileOperatorExecutionOutboxEntry(
                    $"OUT-{nextState.OutboxSequence:000000}",
                    outboxDraft.AggregateType,
                    outboxDraft.AggregateId,
                    outboxDraft.DomainEvent.GetType().Name,
                    outboxDraft.DomainEvent.OccurredAt,
                    SerializeDomainEvent(outboxDraft.DomainEvent),
                    request.CommittedAt));
            }

            PersistAndSwap(nextState);
        }
    }

    /// <summary>
    /// 저장소 JSON 직렬화 옵션을 생성합니다.
    /// </summary>
    /// <returns>파일 저장용 JSON 옵션입니다.</returns>
    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    /// <summary>
    /// 저장소 파일이 없으면 빈 상태를, 있으면 역직렬화된 상태를 반환합니다.
    /// </summary>
    /// <param name="storeFilePath">저장소 파일 경로입니다.</param>
    /// <returns>메모리에서 사용할 현재 저장소 상태입니다.</returns>
    private static FileOperatorExecutionPersistenceState LoadState(string storeFilePath)
    {
        if (!File.Exists(storeFilePath))
        {
            return new FileOperatorExecutionPersistenceState();
        }

        using var stream = File.OpenRead(storeFilePath);
        var state = JsonSerializer.Deserialize<FileOperatorExecutionPersistenceState>(stream, JsonOptions);
        return state ?? new FileOperatorExecutionPersistenceState();
    }

    /// <summary>
    /// 현재 상태를 복사해 메모리와 디스크를 분리된 변경 단위로 다룹니다.
    /// </summary>
    /// <param name="source">복사할 원본 상태입니다.</param>
    /// <returns>변경 가능한 새 상태 복사본입니다.</returns>
    private static FileOperatorExecutionPersistenceState CloneState(FileOperatorExecutionPersistenceState source)
    {
        return new FileOperatorExecutionPersistenceState
        {
            ProductionOrders = new Dictionary<string, ProductionOrderFileSnapshot>(source.ProductionOrders, StringComparer.Ordinal),
            OperationExecutions = new Dictionary<string, OperationExecutionFileSnapshot>(source.OperationExecutions, StringComparer.Ordinal),
            WipUnits = new Dictionary<string, WipUnitFileSnapshot>(source.WipUnits, StringComparer.Ordinal),
            MaterialLots = new Dictionary<string, MaterialLotFileSnapshot>(source.MaterialLots, StringComparer.Ordinal),
            QualityRecords = new Dictionary<string, QualityRecordFileSnapshot>(source.QualityRecords, StringComparer.Ordinal),
            MaterialRequirements = new Dictionary<string, OperationMaterialRequirementFileSnapshot>(source.MaterialRequirements, StringComparer.Ordinal),
            Receipts = new Dictionary<string, CommandReceiptRecord>(source.Receipts, StringComparer.Ordinal),
            ProductionActualsBatches = new Dictionary<string, PreparedProductionActualsBatch>(source.ProductionActualsBatches, StringComparer.Ordinal),
            OutboxEntries = source.OutboxEntries.ToList(),
            OutboxSequence = source.OutboxSequence
        };
    }

    /// <summary>
    /// 상태를 디스크에 안전하게 기록한 뒤 메모리 상태를 교체합니다.
    /// </summary>
    /// <param name="nextState">디스크에 기록할 새 상태입니다.</param>
    private void PersistAndSwap(FileOperatorExecutionPersistenceState nextState)
    {
        PersistState(_storeFilePath, nextState);
        _state = nextState;
    }

    /// <summary>
    /// JSON 파일을 임시 파일 교체 방식으로 원자적으로 갱신합니다.
    /// </summary>
    /// <param name="storeFilePath">저장소 파일 경로입니다.</param>
    /// <param name="state">기록할 상태입니다.</param>
    private static void PersistState(string storeFilePath, FileOperatorExecutionPersistenceState state)
    {
        var directoryPath = Path.GetDirectoryName(storeFilePath)
            ?? throw new InvalidOperationException("The store file path must include a parent directory.");
        var tempFilePath = Path.Combine(directoryPath, Path.GetRandomFileName());

        try
        {
            using (var stream = File.Create(tempFilePath))
            {
                JsonSerializer.Serialize(stream, state, JsonOptions);
            }

            if (File.Exists(storeFilePath))
            {
                File.Replace(tempFilePath, storeFilePath, null);
            }
            else
            {
                File.Move(tempFilePath, storeFilePath);
            }
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    /// <summary>
    /// 저장소 상위 디렉터리를 보장합니다.
    /// </summary>
    /// <param name="storeFilePath">저장소 파일 경로입니다.</param>
    private static void EnsureParentDirectoryExists(string storeFilePath)
    {
        var directoryPath = Path.GetDirectoryName(storeFilePath)
            ?? throw new InvalidOperationException("The store file path must include a parent directory.");
        Directory.CreateDirectory(directoryPath);
    }

    /// <summary>
    /// domain event를 JSON payload로 직렬화합니다.
    /// </summary>
    /// <param name="domainEvent">직렬화할 domain event입니다.</param>
    /// <returns>outbox 저장용 JSON payload입니다.</returns>
    private static string SerializeDomainEvent(Mes.Domain.Abstractions.IDomainEvent domainEvent)
    {
        return JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonOptions);
    }

    /// <summary>
    /// receipt scope를 파일 사전 키로 정규화합니다.
    /// </summary>
    /// <param name="scope">정규화할 receipt scope입니다.</param>
    /// <returns>사전 조회용 키 문자열입니다.</returns>
    private static string CreateReceiptKey(CommandReceiptScope scope)
    {
        return $"{scope.Channel.Trim().ToLowerInvariant()}|{scope.CommandType.Trim()}|{scope.IdempotencyKey.Trim()}";
    }

    /// <summary>
    /// 사전에서 필수 항목을 조회합니다.
    /// </summary>
    /// <typeparam name="TValue">사전 값 형식입니다.</typeparam>
    /// <param name="source">조회 대상 사전입니다.</param>
    /// <param name="key">조회 키입니다.</param>
    /// <param name="parameterName">예외 메시지에 사용할 파라미터 이름입니다.</param>
    /// <returns>조회된 값입니다.</returns>
    private static TValue Require<TValue>(
        IReadOnlyDictionary<string, TValue> source,
        string key,
        string parameterName)
    {
        if (!source.TryGetValue(key, out var value))
        {
            throw CreateNotFoundException(parameterName, key);
        }

        return value;
    }

    /// <summary>
    /// file-backed 저장소 조회 miss를 operator-execution not-found 예외로 변환합니다.
    /// </summary>
    /// <param name="parameterName">조회에 사용한 식별자 이름입니다.</param>
    /// <param name="key">조회에 사용한 식별자 값입니다.</param>
    /// <returns>host가 problem details로 승격할 수 있는 not-found 예외입니다.</returns>
    private static OperatorExecutionNotFoundException CreateNotFoundException(string parameterName, string key)
    {
        var aggregateType = ResolveAggregateType(parameterName);
        return new OperatorExecutionNotFoundException(
            $"Could not find {parameterName} '{key}'.",
            new OperatorExecutionErrorContext(
                aggregateType,
                key.Trim(),
                null,
                null));
    }

    /// <summary>
    /// 저장소 파라미터 이름을 problem details용 aggregate 이름으로 정규화합니다.
    /// </summary>
    /// <param name="parameterName">조회 파라미터 이름입니다.</param>
    /// <returns>알려진 aggregate 이름 또는 원본 파라미터 이름입니다.</returns>
    private static string ResolveAggregateType(string parameterName)
    {
        return parameterName switch
        {
            "productionOrderId" => nameof(ProductionOrder),
            "operationExecutionId" or "currentOperationExecutionId" => nameof(OperationExecution),
            "wipUnitId" => nameof(WipUnit),
            "materialLotId" => nameof(MaterialLot),
            "qualityRecordId" => nameof(QualityRecord),
            _ => parameterName
        };
    }

    /// <summary>
    /// 완료 진행률 계산에서 공정을 완료 상태로 간주할지 판별합니다.
    /// </summary>
    /// <param name="status">판별할 공정 실행 상태입니다.</param>
    /// <returns>Release 1 기준 완료 상태면 <see langword="true"/>입니다.</returns>
    private static bool IsCompletedForOrderProgression(OperationExecutionStatus status)
    {
        return status == OperationExecutionStatus.Done;
    }

    /// <summary>
    /// 필수 문자열 입력을 정규화합니다.
    /// </summary>
    /// <param name="value">정규화할 문자열입니다.</param>
    /// <param name="parameterName">예외 메시지에 사용할 파라미터 이름입니다.</param>
    /// <returns>trim 처리된 문자열입니다.</returns>
    private static string NormalizeRequired(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Required string value cannot be empty.", parameterName);
        }

        return value.Trim();
    }
}
