using Mes.Application.Contracts.Common;
using Mes.Application.Contracts.OperatorExecution;
using Mes.Application.Idempotency;
using Mes.Application.OperatorExecution;
using Mes.Domain.Abstractions;
using Mes.Domain.Aggregates;
using Mes.Domain.Entities;
using Mes.Domain.ValueObjects;

namespace Mes.Infrastructure.OperatorExecution.InMemory;

/// <summary>
/// 운영자 실행 command 포트를 in-memory 기준 저장소로 구현합니다.
/// </summary>
public sealed class InMemoryOperatorExecutionCommandAdapter : IOperatorExecutionCommandPort
{
    private readonly CanonicalCommandFingerprintBuilder _fingerprintBuilder;
    private readonly InMemoryOperatorExecutionStore _store;

    /// <summary>
    /// in-memory command 어댑터를 초기화합니다.
    /// </summary>
    /// <param name="store">기준 저장소입니다.</param>
    /// <param name="fingerprintBuilder">receipt scope 계산기입니다.</param>
    public InMemoryOperatorExecutionCommandAdapter(
        InMemoryOperatorExecutionStore store,
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
    /// <returns>공정 시작 상태 묶음입니다.</returns>
    public Task<StartOperationCommandState> LoadStateAsync(
        StartOperationCommandContract command,
        CancellationToken cancellationToken = default)
    {
        var operation = _store.GetOperationExecution(command.Payload.OperationExecutionId);
        var order = _store.GetProductionOrder(command.Payload.ProductionOrderId);
        return Task.FromResult(new StartOperationCommandState(order, operation));
    }

    /// <summary>
    /// 자재 스캔 검증 상태를 로드합니다.
    /// </summary>
    /// <param name="command">자재 스캔 검증 command입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>자재 스캔 검증 상태 묶음입니다.</returns>
    public Task<RecordMaterialScanCommandState> LoadStateAsync(
        RecordMaterialScanCommandContract command,
        CancellationToken cancellationToken = default)
    {
        var operation = _store.GetOperationExecution(command.Payload.OperationExecutionId);
        return Task.FromResult(new RecordMaterialScanCommandState(
            operation,
            _store.GetWipUnit(command.Payload.WipUnitId),
            _store.GetMaterialLot(command.Payload.MaterialLotId),
            _store.GetMaterialRequirements(operation.Id.ToString())
                .Select(requirement => requirement.MaterialCode)
                .ToList()));
    }

    /// <summary>
    /// 자재 소모 상태를 로드합니다.
    /// </summary>
    /// <param name="command">자재 소모 command입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>자재 소모 상태 묶음입니다.</returns>
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
    /// <returns>hold 설정 상태 묶음입니다.</returns>
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
    /// <returns>hold 해제 상태 묶음입니다.</returns>
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
    /// <returns>품질 결과 상태 묶음입니다.</returns>
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
    /// <returns>공정 완료 상태 묶음입니다.</returns>
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
    /// 수락된 command의 side effect를 저장소에 커밋합니다.
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

        var aggregates = GetAggregates(request.State);
        var outboxEntries = aggregates
            .SelectMany(CreateOutboxEntryDrafts)
            .ToList();

        _store.Commit(new InMemoryOperatorExecutionWriteSet(
            request.ReceiptToStore,
            request.PreparedBatch,
            outboxEntries,
            request.PersistedAt));

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
    /// <returns>hold 설정 상태 묶음입니다.</returns>
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
    /// <returns>hold 해제 상태 묶음입니다.</returns>
    private ReleaseHoldCommandState CreateQualityReleaseState(string qualityRecordId)
    {
        var qualityRecord = _store.GetQualityRecord(qualityRecordId);
        var wipUnit = _store.GetWipUnit(qualityRecord.WipUnitId.ToString());
        var operation = LoadLinkedOperation(wipUnit);
        return new ReleaseHoldCommandState(null, null, qualityRecord, operation);
    }

    /// <summary>
    /// WIP와 연결된 현재 공정 실행을 조회합니다.
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
    /// 상태 묶음에서 저장 대상 aggregate를 추출합니다.
    /// </summary>
    /// <typeparam name="TState">상태 묶음 형식입니다.</typeparam>
    /// <param name="state">aggregate를 추출할 상태 묶음입니다.</param>
    /// <returns>중복 제거된 aggregate 목록입니다.</returns>
    private static IReadOnlyList<object> GetAggregates<TState>(TState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var aggregates = state switch
        {
            StartOperationCommandState start => [start.ProductionOrder, start.OperationExecution],
            RecordMaterialScanCommandState materialScan => [materialScan.OperationExecution, materialScan.MaterialLot],
            RecordMaterialConsumptionCommandState material => [material.OperationExecution, material.MaterialLot],
            PlaceHoldCommandState placeHold => new object?[]
            {
                placeHold.OperationExecution,
                placeHold.QualityRecord,
                placeHold.LinkedOperationExecution
            },
            ReleaseHoldCommandState releaseHold => new object?[]
            {
                releaseHold.OperationExecution,
                releaseHold.QualityRecord,
                releaseHold.LinkedOperationExecution
            },
            RecordQualityResultCommandState quality => [quality.QualityRecord, quality.OperationExecution],
            CompleteOperationCommandState complete => [complete.ProductionOrder, complete.OperationExecution],
            _ => throw new InvalidOperationException($"Unsupported command state type: {state.GetType().Name}.")
        };

        return aggregates
            .Where(aggregate => aggregate is not null)
            .Select(aggregate => aggregate!)
            .Distinct(AggregateReferenceComparer.Instance)
            .ToList();
    }

    /// <summary>
    /// aggregate의 domain event를 outbox entry 초안으로 변환합니다.
    /// </summary>
    /// <param name="aggregate">outbox entry를 생성할 aggregate입니다.</param>
    /// <returns>aggregate가 가진 domain event 초안 목록입니다.</returns>
    private static IEnumerable<InMemoryOutboxEntryDraft> CreateOutboxEntryDrafts(object aggregate)
    {
        var domainEvents = GetDomainEvents(aggregate);
        var aggregateType = aggregate.GetType().Name;
        var aggregateId = GetAggregateId(aggregate);

        return domainEvents.Select(domainEvent => new InMemoryOutboxEntryDraft(
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
    /// aggregate의 domain event 컬렉션을 반환합니다.
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
    /// aggregate reference 중복 제거용 비교기를 제공합니다.
    /// </summary>
    private sealed class AggregateReferenceComparer : IEqualityComparer<object>
    {
        /// <summary>
        /// 전역 비교기 인스턴스입니다.
        /// </summary>
        public static AggregateReferenceComparer Instance { get; } = new();

        /// <summary>
        /// 두 aggregate reference가 동일한 인스턴스인지 비교합니다.
        /// </summary>
        /// <param name="x">왼쪽 aggregate입니다.</param>
        /// <param name="y">오른쪽 aggregate입니다.</param>
        /// <returns>같은 인스턴스면 <see langword="true"/>입니다.</returns>
        public new bool Equals(object? x, object? y)
        {
            return ReferenceEquals(x, y);
        }

        /// <summary>
        /// aggregate reference의 런타임 hash code를 반환합니다.
        /// </summary>
        /// <param name="obj">hash를 계산할 aggregate입니다.</param>
        /// <returns>reference 기준 hash code입니다.</returns>
        public int GetHashCode(object obj)
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
