using Mes.Application.Contracts.Common;
using Mes.Application.Contracts.OperatorExecution;
using Mes.Application.Idempotency;
using Mes.Application.OperatorExecution;
using Mes.Domain.Abstractions;
using Mes.Domain.Aggregates;
using Mes.Domain.Entities;

namespace Mes.Infrastructure.OperatorExecution.Sqlite;

/// <summary>
/// operator-execution command port를 SQLite 기반 durable 저장소로 구현합니다.
/// </summary>
public sealed class SqliteOperatorExecutionCommandAdapter : IOperatorExecutionCommandPort
{
    private readonly CanonicalCommandFingerprintBuilder _fingerprintBuilder;
    private readonly SqliteOperatorExecutionStore _store;

    /// <summary>
    /// SQLite 기반 command adapter를 초기화합니다.
    /// </summary>
    /// <param name="store">SQLite 기반 저장소입니다.</param>
    /// <param name="fingerprintBuilder">receipt scope 계산기입니다.</param>
    public SqliteOperatorExecutionCommandAdapter(
        SqliteOperatorExecutionStore store,
        CanonicalCommandFingerprintBuilder fingerprintBuilder)
    {
        _store = store;
        _fingerprintBuilder = fingerprintBuilder;
    }

    /// <summary>
    /// 기존 receipt를 조회합니다.
    /// </summary>
    /// <typeparam name="TPayload">command payload 형식입니다.</typeparam>
    /// <param name="command">조회할 command envelope입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>기존 receipt가 있으면 반환합니다.</returns>
    public Task<CommandReceiptRecord?> LoadReceiptAsync<TPayload>(
        BffCommandEnvelope<TPayload> command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Task.FromResult(_store.FindReceipt(_fingerprintBuilder.CreateScope(command)));
    }

    /// <summary>
    /// 공정 시작 상태를 로드합니다.
    /// </summary>
    /// <param name="command">공정 시작 command입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>공정 시작 처리 상태입니다.</returns>
    public Task<StartOperationCommandState> LoadStateAsync(
        StartOperationCommandContract command,
        CancellationToken cancellationToken = default)
    {
        var operation = _store.GetOperationExecution(command.Payload.OperationExecutionId);
        var order = _store.GetProductionOrder(command.Payload.ProductionOrderId);
        return Task.FromResult(new StartOperationCommandState(order, operation));
    }

    /// <summary>
    /// 자재 소모 상태를 로드합니다.
    /// </summary>
    /// <param name="command">자재 소모 command입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>자재 소모 처리 상태입니다.</returns>
    public Task<RecordMaterialConsumptionCommandState> LoadStateAsync(
        RecordMaterialConsumptionCommandContract command,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new RecordMaterialConsumptionCommandState(
            _store.GetOperationExecution(command.Payload.OperationExecutionId),
            _store.GetWipUnit(command.Payload.WipUnitId),
            _store.GetMaterialLot(command.Payload.MaterialLotId)));
    }

    /// <summary>
    /// hold 설정 상태를 로드합니다.
    /// </summary>
    /// <param name="command">hold 설정 command입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>hold 설정 처리 상태입니다.</returns>
    public Task<PlaceHoldCommandState> LoadStateAsync(
        PlaceHoldCommandContract command,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(command.Payload.SubjectType switch
        {
            HoldSubjectTypeValues.OperationExecution => new PlaceHoldCommandState(
                _store.GetOperationExecution(command.Payload.SubjectId),
                null,
                null),
            HoldSubjectTypeValues.WipUnit => new PlaceHoldCommandState(
                null,
                _store.GetWipUnit(command.Payload.SubjectId),
                null),
            HoldSubjectTypeValues.QualityRecord => CreateQualityHoldState(command.Payload.SubjectId),
            _ => throw new InvalidOperationException($"Unsupported hold subject type: {command.Payload.SubjectType}.")
        });
    }

    /// <summary>
    /// hold 해제 상태를 로드합니다.
    /// </summary>
    /// <param name="command">hold 해제 command입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>hold 해제 처리 상태입니다.</returns>
    public Task<ReleaseHoldCommandState> LoadStateAsync(
        ReleaseHoldCommandContract command,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(command.Payload.SubjectType switch
        {
            HoldSubjectTypeValues.OperationExecution => new ReleaseHoldCommandState(
                _store.GetOperationExecution(command.Payload.SubjectId),
                null,
                null),
            HoldSubjectTypeValues.WipUnit => new ReleaseHoldCommandState(
                null,
                _store.GetWipUnit(command.Payload.SubjectId),
                null),
            HoldSubjectTypeValues.QualityRecord => CreateQualityReleaseState(command.Payload.SubjectId),
            _ => throw new InvalidOperationException($"Unsupported hold subject type: {command.Payload.SubjectType}.")
        });
    }

    /// <summary>
    /// 품질 결과 기록 상태를 로드합니다.
    /// </summary>
    /// <param name="command">품질 결과 command입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>품질 결과 처리 상태입니다.</returns>
    public Task<RecordQualityResultCommandState> LoadStateAsync(
        RecordQualityResultCommandContract command,
        CancellationToken cancellationToken = default)
    {
        var qualityRecord = _store.GetQualityRecord(command.Payload.QualityRecordId);
        var wipUnit = _store.GetWipUnit(command.Payload.WipUnitId);
        var operationExecutionId = wipUnit.CurrentOperationExecutionId?.ToString()
            ?? throw new InvalidOperationException("The target WIP unit is not linked to an active operation execution.");

        return Task.FromResult(new RecordQualityResultCommandState(
            qualityRecord,
            wipUnit,
            _store.GetOperationExecution(operationExecutionId)));
    }

    /// <summary>
    /// 공정 완료 상태를 로드합니다.
    /// </summary>
    /// <param name="command">공정 완료 command입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>공정 완료 처리 상태입니다.</returns>
    public Task<CompleteOperationCommandState> LoadStateAsync(
        CompleteOperationCommandContract command,
        CancellationToken cancellationToken = default)
    {
        var operation = _store.GetOperationExecution(command.Payload.OperationExecutionId);
        var order = _store.GetProductionOrder(operation.ProductionOrderId.ToString());
        return Task.FromResult(new CompleteOperationCommandState(
            order,
            operation,
            _store.GetOrderCompletionProgress(order.Id.ToString(), operation.Id.ToString())));
    }

    /// <summary>
    /// 수락된 command의 side effect를 SQLite 저장소에 커밋합니다.
    /// </summary>
    /// <typeparam name="TState">저장할 상태 묶음 형식입니다.</typeparam>
    /// <param name="request">저장 요청입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>비동기 저장 작업입니다.</returns>
    public Task SaveAsync<TState>(
        PersistOperatorExecutionCommandRequest<TState> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var state = request.State ?? throw new ArgumentNullException(nameof(request.State));
        var aggregates = GetAggregates(state);

        _store.Commit(new SqliteOperatorExecutionCommitRequest
        {
            ProductionOrders = GetValues<ProductionOrder>(state),
            OperationExecutions = GetValues<OperationExecution>(state),
            WipUnits = GetValues<WipUnit>(state),
            MaterialLots = GetValues<MaterialLot>(state),
            QualityRecords = GetValues<QualityRecord>(state),
            Receipt = request.ReceiptToStore,
            PreparedBatch = request.PreparedBatch,
            OutboxEntries = aggregates.SelectMany(CreateOutboxEntryDrafts).ToList(),
            CommittedAt = request.PersistedAt
        });

        foreach (var aggregate in aggregates)
        {
            ClearDomainEvents(aggregate);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 품질 기록 subject에 대응하는 hold 상태를 조합합니다.
    /// </summary>
    /// <param name="qualityRecordId">품질 기록 식별자입니다.</param>
    /// <returns>hold 설정 처리 상태입니다.</returns>
    private PlaceHoldCommandState CreateQualityHoldState(string qualityRecordId)
    {
        var qualityRecord = _store.GetQualityRecord(qualityRecordId);
        var wipUnit = _store.GetWipUnit(qualityRecord.WipUnitId.ToString());
        var operation = LoadLinkedOperation(wipUnit);
        return new PlaceHoldCommandState(null, null, qualityRecord, operation);
    }

    /// <summary>
    /// 품질 기록 subject에 대응하는 release 상태를 조합합니다.
    /// </summary>
    /// <param name="qualityRecordId">품질 기록 식별자입니다.</param>
    /// <returns>hold 해제 처리 상태입니다.</returns>
    private ReleaseHoldCommandState CreateQualityReleaseState(string qualityRecordId)
    {
        var qualityRecord = _store.GetQualityRecord(qualityRecordId);
        var wipUnit = _store.GetWipUnit(qualityRecord.WipUnitId.ToString());
        var operation = LoadLinkedOperation(wipUnit);
        return new ReleaseHoldCommandState(null, null, qualityRecord, operation);
    }

    /// <summary>
    /// WIP에 연결된 현재 공정 실행을 조회합니다.
    /// </summary>
    /// <param name="wipUnit">연결 정보를 가진 WIP입니다.</param>
    /// <returns>연결된 공정 실행 aggregate입니다.</returns>
    private OperationExecution LoadLinkedOperation(WipUnit wipUnit)
    {
        var operationExecutionId = wipUnit.CurrentOperationExecutionId?.ToString()
            ?? throw new InvalidOperationException("The linked WIP unit does not point to an active operation execution.");

        return _store.GetOperationExecution(operationExecutionId);
    }

    /// <summary>
    /// 상태 묶음에서 특정 형식의 변경 대상을 추출합니다.
    /// </summary>
    /// <typeparam name="TValue">추출할 형식입니다.</typeparam>
    /// <param name="state">변경 대상 상태 묶음입니다.</param>
    /// <returns>중복을 제거한 변경 대상 목록입니다.</returns>
    private static IReadOnlyCollection<TValue> GetValues<TValue>(object state)
        where TValue : class
    {
        ArgumentNullException.ThrowIfNull(state);

        var values = state switch
        {
            StartOperationCommandState start => [start.ProductionOrder, start.OperationExecution],
            RecordMaterialConsumptionCommandState material => [material.OperationExecution, material.WipUnit, material.MaterialLot],
            PlaceHoldCommandState placeHold => new object?[]
            {
                placeHold.OperationExecution,
                placeHold.WipUnit,
                placeHold.QualityRecord,
                placeHold.LinkedOperationExecution
            },
            ReleaseHoldCommandState releaseHold => new object?[]
            {
                releaseHold.OperationExecution,
                releaseHold.WipUnit,
                releaseHold.QualityRecord,
                releaseHold.LinkedOperationExecution
            },
            RecordQualityResultCommandState quality => [quality.QualityRecord, quality.WipUnit, quality.OperationExecution],
            CompleteOperationCommandState complete => [complete.ProductionOrder, complete.OperationExecution],
            _ => throw new InvalidOperationException($"Unsupported command state type: {state.GetType().Name}.")
        };

        return values
            .OfType<TValue>()
            .Distinct(ReferenceEqualityComparer<TValue>.Instance)
            .ToList();
    }

    /// <summary>
    /// 상태 묶음에서 outbox 대상 aggregate 목록을 추출합니다.
    /// </summary>
    /// <param name="state">aggregate를 추출할 상태 묶음입니다.</param>
    /// <returns>중복을 제거한 aggregate 목록입니다.</returns>
    private static IReadOnlyList<object> GetAggregates(object state)
    {
        return GetValues<object>(state)
            .Where(value => value is ProductionOrder or OperationExecution or MaterialLot or QualityRecord)
            .ToList();
    }

    /// <summary>
    /// aggregate의 domain event를 outbox 초안으로 변환합니다.
    /// </summary>
    /// <param name="aggregate">outbox 초안을 만들 aggregate입니다.</param>
    /// <returns>aggregate가 가진 domain event 초안 목록입니다.</returns>
    private static IEnumerable<SqliteOperatorExecutionOutboxDraft> CreateOutboxEntryDrafts(object aggregate)
    {
        var aggregateType = aggregate.GetType().Name;
        var aggregateId = GetAggregateId(aggregate);

        return GetDomainEvents(aggregate).Select(domainEvent => new SqliteOperatorExecutionOutboxDraft(
            aggregateType,
            aggregateId,
            domainEvent));
    }

    /// <summary>
    /// aggregate 식별자를 문자열로 추출합니다.
    /// </summary>
    /// <param name="aggregate">식별자를 추출할 aggregate입니다.</param>
    /// <returns>aggregate 식별자 문자열입니다.</returns>
    private static string GetAggregateId(object aggregate)
    {
        return aggregate switch
        {
            ProductionOrder order => order.Id.ToString(),
            OperationExecution operationExecution => operationExecution.Id.ToString(),
            MaterialLot materialLot => materialLot.Id.ToString(),
            QualityRecord qualityRecord => qualityRecord.Id.ToString(),
            _ => throw new InvalidOperationException($"Unsupported aggregate type: {aggregate.GetType().Name}.")
        };
    }

    /// <summary>
    /// aggregate가 가진 domain event 컬렉션을 반환합니다.
    /// </summary>
    /// <param name="aggregate">event를 읽을 aggregate입니다.</param>
    /// <returns>aggregate의 현재 domain event 컬렉션입니다.</returns>
    private static IReadOnlyCollection<IDomainEvent> GetDomainEvents(object aggregate)
    {
        return aggregate switch
        {
            ProductionOrder order => order.DomainEvents,
            OperationExecution operationExecution => operationExecution.DomainEvents,
            MaterialLot materialLot => materialLot.DomainEvents,
            QualityRecord qualityRecord => qualityRecord.DomainEvents,
            _ => throw new InvalidOperationException($"Unsupported aggregate type: {aggregate.GetType().Name}.")
        };
    }

    /// <summary>
    /// aggregate의 domain event 컬렉션을 비웁니다.
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
                throw new InvalidOperationException($"Unsupported aggregate type: {aggregate.GetType().Name}.");
        }
    }

    /// <summary>
    /// 참조 동일성 기준 중복 제거 비교기입니다.
    /// </summary>
    /// <typeparam name="TValue">비교할 참조 형식입니다.</typeparam>
    private sealed class ReferenceEqualityComparer<TValue> : IEqualityComparer<TValue>
        where TValue : class
    {
        /// <summary>
        /// 전역 비교기 인스턴스입니다.
        /// </summary>
        public static ReferenceEqualityComparer<TValue> Instance { get; } = new();

        /// <summary>
        /// 두 객체가 같은 참조인지 비교합니다.
        /// </summary>
        /// <param name="x">왼쪽 객체입니다.</param>
        /// <param name="y">오른쪽 객체입니다.</param>
        /// <returns>같은 참조면 <see langword="true"/>입니다.</returns>
        public bool Equals(TValue? x, TValue? y)
        {
            return ReferenceEquals(x, y);
        }

        /// <summary>
        /// 참조 기반 hash code를 반환합니다.
        /// </summary>
        /// <param name="obj">hash를 계산할 객체입니다.</param>
        /// <returns>참조 기반 hash code입니다.</returns>
        public int GetHashCode(TValue obj)
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
