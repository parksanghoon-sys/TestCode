using Mes.Application.Idempotency;
using Mes.Application.OperatorExecution;
using Mes.Application.OperatorExecution.WorkQueue;
using Mes.Domain.Abstractions;
using Mes.Domain.Aggregates;
using Mes.Domain.Entities;
using Mes.Domain.Statuses;
using Mes.Domain.ValueObjects;

namespace Mes.Infrastructure.OperatorExecution.FileStore;

/// <summary>
/// 파일 저장소에 기록되는 operator-execution 전체 상태 스냅샷입니다.
/// </summary>
internal sealed class FileOperatorExecutionPersistenceState
{
    /// <summary>
    /// 생산 오더 스냅샷 사전입니다.
    /// </summary>
    public Dictionary<string, ProductionOrderFileSnapshot> ProductionOrders { get; init; } = new(StringComparer.Ordinal);

    /// <summary>
    /// 공정 실행 스냅샷 사전입니다.
    /// </summary>
    public Dictionary<string, OperationExecutionFileSnapshot> OperationExecutions { get; init; } = new(StringComparer.Ordinal);

    /// <summary>
    /// WIP 스냅샷 사전입니다.
    /// </summary>
    public Dictionary<string, WipUnitFileSnapshot> WipUnits { get; init; } = new(StringComparer.Ordinal);

    /// <summary>
    /// 자재 lot 스냅샷 사전입니다.
    /// </summary>
    public Dictionary<string, MaterialLotFileSnapshot> MaterialLots { get; init; } = new(StringComparer.Ordinal);

    /// <summary>
    /// 품질 기록 스냅샷 사전입니다.
    /// </summary>
    public Dictionary<string, QualityRecordFileSnapshot> QualityRecords { get; init; } = new(StringComparer.Ordinal);

    /// <summary>
    /// 자재 요구 스냅샷 사전입니다.
    /// </summary>
    public Dictionary<string, OperationMaterialRequirementFileSnapshot> MaterialRequirements { get; init; } = new(StringComparer.Ordinal);

    /// <summary>
    /// command receipt 사전입니다.
    /// </summary>
    public Dictionary<string, CommandReceiptRecord> Receipts { get; init; } = new(StringComparer.Ordinal);

    /// <summary>
    /// production actuals batch 사전입니다.
    /// </summary>
    public Dictionary<string, PreparedProductionActualsBatch> ProductionActualsBatches { get; init; } = new(StringComparer.Ordinal);

    /// <summary>
    /// domain outbox 항목 목록입니다.
    /// </summary>
    public List<FileOperatorExecutionOutboxEntry> OutboxEntries { get; init; } = [];

    /// <summary>
    /// 다음 outbox 식별자 생성에 사용하는 시퀀스입니다.
    /// </summary>
    public long OutboxSequence { get; set; }
}

/// <summary>
/// 생산 오더 파일 스냅샷입니다.
/// </summary>
/// <param name="ProductionOrderId">생산 오더 식별자입니다.</param>
/// <param name="ItemCode">생산 품목 코드입니다.</param>
/// <param name="RouteRevision">라우팅 리비전입니다.</param>
/// <param name="Status">현재 상태입니다.</param>
/// <param name="ReleasedAt">릴리즈 시각입니다.</param>
/// <param name="OperationIds">연결된 공정 실행 식별자 목록입니다.</param>
internal sealed record ProductionOrderFileSnapshot(
    string ProductionOrderId,
    string ItemCode,
    string RouteRevision,
    ProductionOrderStatus Status,
    DateTimeOffset ReleasedAt,
    IReadOnlyCollection<string> OperationIds)
{
    /// <summary>
    /// aggregate에서 파일 스냅샷을 생성합니다.
    /// </summary>
    /// <param name="order">스냅샷으로 변환할 생산 오더입니다.</param>
    /// <returns>파일 저장용 생산 오더 스냅샷입니다.</returns>
    public static ProductionOrderFileSnapshot FromAggregate(ProductionOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);

        return new ProductionOrderFileSnapshot(
            order.Id.ToString(),
            order.ItemCode,
            order.RouteRevision,
            order.Status,
            order.ReleasedAt,
            order.OperationIds.Select(operationId => operationId.ToString()).ToList());
    }

    /// <summary>
    /// 파일 스냅샷에서 생산 오더 aggregate를 복원합니다.
    /// </summary>
    /// <returns>복원된 생산 오더 aggregate입니다.</returns>
    public ProductionOrder Restore()
    {
        return ProductionOrder.Restore(new ProductionOrderRestoreState(
            new ProductionOrderId(ProductionOrderId),
            ItemCode,
            RouteRevision,
            Status,
            ReleasedAt,
            OperationIds.Select(operationId => new OperationExecutionId(operationId)).ToList()));
    }
}

/// <summary>
/// 공정 실행 파일 스냅샷입니다.
/// </summary>
/// <param name="OperationExecutionId">공정 실행 식별자입니다.</param>
/// <param name="ProductionOrderId">상위 생산 오더 식별자입니다.</param>
/// <param name="OperationSequence">공정 순번입니다.</param>
/// <param name="QuantityUnit">수량 단위입니다.</param>
/// <param name="Status">현재 공정 상태입니다.</param>
/// <param name="StationId">현재 스테이션 식별자입니다.</param>
/// <param name="HoldReason">현재 hold 사유입니다.</param>
/// <param name="StatusBeforeHold">hold 직전 상태입니다.</param>
/// <param name="HoldSourceType">hold 출처 유형입니다.</param>
/// <param name="HoldSourceId">hold 출처 식별자입니다.</param>
/// <param name="StartedAt">시작 시각입니다.</param>
/// <param name="CompletedAt">완료 시각입니다.</param>
/// <param name="GoodQuantity">누적 양품 수량입니다.</param>
/// <param name="ScrapQuantity">누적 불량 수량입니다.</param>
internal sealed record OperationExecutionFileSnapshot(
    string OperationExecutionId,
    string ProductionOrderId,
    int OperationSequence,
    string QuantityUnit,
    OperationExecutionStatus Status,
    string? StationId,
    string? HoldReason,
    OperationExecutionStatus? StatusBeforeHold,
    string? HoldSourceType,
    string? HoldSourceId,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    MeasuredQuantity GoodQuantity,
    MeasuredQuantity ScrapQuantity)
{
    /// <summary>
    /// aggregate에서 파일 스냅샷을 생성합니다.
    /// </summary>
    /// <param name="operation">스냅샷으로 변환할 공정 실행입니다.</param>
    /// <returns>파일 저장용 공정 실행 스냅샷입니다.</returns>
    public static OperationExecutionFileSnapshot FromAggregate(OperationExecution operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return new OperationExecutionFileSnapshot(
            operation.Id.ToString(),
            operation.ProductionOrderId.ToString(),
            operation.OperationSequence,
            operation.QuantityUnit,
            operation.Status,
            operation.StationId?.ToString(),
            operation.HoldReason,
            operation.StatusBeforeHold,
            operation.HoldSourceType,
            operation.HoldSourceId,
            operation.StartedAt,
            operation.CompletedAt,
            operation.GoodQuantity,
            operation.ScrapQuantity);
    }

    /// <summary>
    /// 파일 스냅샷에서 공정 실행 aggregate를 복원합니다.
    /// </summary>
    /// <returns>복원된 공정 실행 aggregate입니다.</returns>
    public OperationExecution Restore()
    {
        return OperationExecution.Restore(new OperationExecutionRestoreState(
            new OperationExecutionId(OperationExecutionId),
            new ProductionOrderId(ProductionOrderId),
            OperationSequence,
            QuantityUnit,
            Status,
            string.IsNullOrWhiteSpace(StationId) ? null : new StationId(StationId),
            HoldReason,
            StatusBeforeHold,
            HoldSourceType,
            HoldSourceId,
            StartedAt,
            CompletedAt,
            GoodQuantity,
            ScrapQuantity));
    }
}

/// <summary>
/// WIP 파일 스냅샷입니다.
/// </summary>
/// <param name="WipUnitId">WIP 식별자입니다.</param>
/// <param name="ProductCode">생산 품목 코드입니다.</param>
/// <param name="Status">현재 WIP 상태입니다.</param>
/// <param name="CurrentOperationExecutionId">현재 연결된 공정 실행 식별자입니다.</param>
/// <param name="HoldReason">현재 hold 사유입니다.</param>
/// <param name="StatusBeforeHold">hold 직전 WIP 상태입니다.</param>
internal sealed record WipUnitFileSnapshot(
    string WipUnitId,
    string ProductCode,
    WipUnitStatus Status,
    string? CurrentOperationExecutionId,
    string? HoldReason,
    WipUnitStatus? StatusBeforeHold)
{
    /// <summary>
    /// 엔티티에서 파일 스냅샷을 생성합니다.
    /// </summary>
    /// <param name="wipUnit">스냅샷으로 변환할 WIP 엔티티입니다.</param>
    /// <returns>파일 저장용 WIP 스냅샷입니다.</returns>
    public static WipUnitFileSnapshot FromEntity(WipUnit wipUnit)
    {
        ArgumentNullException.ThrowIfNull(wipUnit);

        return new WipUnitFileSnapshot(
            wipUnit.Id.ToString(),
            wipUnit.ProductCode,
            wipUnit.Status,
            wipUnit.CurrentOperationExecutionId?.ToString(),
            wipUnit.HoldReason,
            wipUnit.StatusBeforeHold);
    }

    /// <summary>
    /// 파일 스냅샷에서 WIP 엔티티를 복원합니다.
    /// </summary>
    /// <returns>복원된 WIP 엔티티입니다.</returns>
    public WipUnit Restore()
    {
        return WipUnit.Restore(new WipUnitRestoreState(
            new WipUnitId(WipUnitId),
            ProductCode,
            Status,
            string.IsNullOrWhiteSpace(CurrentOperationExecutionId) ? null : new OperationExecutionId(CurrentOperationExecutionId),
            HoldReason,
            StatusBeforeHold));
    }
}

/// <summary>
/// 자재 lot 파일 스냅샷입니다.
/// </summary>
/// <param name="MaterialLotId">자재 lot 식별자입니다.</param>
/// <param name="MaterialCode">자재 코드입니다.</param>
/// <param name="Status">현재 자재 lot 상태입니다.</param>
/// <param name="AvailableQuantity">가용 수량입니다.</param>
/// <param name="ConsumedQuantity">누적 소모 수량입니다.</param>
/// <param name="ReturnedQuantity">누적 반납 수량입니다.</param>
/// <param name="BlockReason">현재 block 사유입니다.</param>
/// <param name="GenealogyLinks">누적 genealogy link 목록입니다.</param>
internal sealed record MaterialLotFileSnapshot(
    string MaterialLotId,
    string MaterialCode,
    MaterialLotStatus Status,
    MeasuredQuantity AvailableQuantity,
    MeasuredQuantity ConsumedQuantity,
    MeasuredQuantity ReturnedQuantity,
    string? BlockReason,
    IReadOnlyCollection<GenealogyLink> GenealogyLinks)
{
    /// <summary>
    /// aggregate에서 파일 스냅샷을 생성합니다.
    /// </summary>
    /// <param name="materialLot">스냅샷으로 변환할 자재 lot입니다.</param>
    /// <returns>파일 저장용 자재 lot 스냅샷입니다.</returns>
    public static MaterialLotFileSnapshot FromAggregate(MaterialLot materialLot)
    {
        ArgumentNullException.ThrowIfNull(materialLot);

        return new MaterialLotFileSnapshot(
            materialLot.Id.ToString(),
            materialLot.MaterialCode,
            materialLot.Status,
            materialLot.AvailableQuantity,
            materialLot.ConsumedQuantity,
            materialLot.ReturnedQuantity,
            materialLot.BlockReason,
            materialLot.GenealogyLinks.ToList());
    }

    /// <summary>
    /// 파일 스냅샷에서 자재 lot aggregate를 복원합니다.
    /// </summary>
    /// <returns>복원된 자재 lot aggregate입니다.</returns>
    public MaterialLot Restore()
    {
        return MaterialLot.Restore(new MaterialLotRestoreState(
            new MaterialLotId(MaterialLotId),
            MaterialCode,
            Status,
            AvailableQuantity,
            ConsumedQuantity,
            ReturnedQuantity,
            BlockReason,
            GenealogyLinks.ToList()));
    }
}

/// <summary>
/// 품질 기록 파일 스냅샷입니다.
/// </summary>
/// <param name="QualityRecordId">품질 기록 식별자입니다.</param>
/// <param name="WipUnitId">대상 WIP 식별자입니다.</param>
/// <param name="InspectionCode">검사 항목 코드입니다.</param>
/// <param name="Status">현재 품질 기록 상태입니다.</param>
/// <param name="DecisionStatus">마지막 품질 판정입니다.</param>
/// <param name="HoldReason">현재 hold 사유입니다.</param>
/// <param name="DecisionNote">마지막 판정 메모입니다.</param>
internal sealed record QualityRecordFileSnapshot(
    string QualityRecordId,
    string WipUnitId,
    string InspectionCode,
    QualityRecordStatus Status,
    QualityDecisionStatus? DecisionStatus,
    string? HoldReason,
    string? DecisionNote)
{
    /// <summary>
    /// aggregate에서 파일 스냅샷을 생성합니다.
    /// </summary>
    /// <param name="qualityRecord">스냅샷으로 변환할 품질 기록입니다.</param>
    /// <returns>파일 저장용 품질 기록 스냅샷입니다.</returns>
    public static QualityRecordFileSnapshot FromAggregate(QualityRecord qualityRecord)
    {
        ArgumentNullException.ThrowIfNull(qualityRecord);

        return new QualityRecordFileSnapshot(
            qualityRecord.Id.ToString(),
            qualityRecord.WipUnitId.ToString(),
            qualityRecord.InspectionCode,
            qualityRecord.Status,
            qualityRecord.DecisionStatus,
            qualityRecord.HoldReason,
            qualityRecord.DecisionNote);
    }

    /// <summary>
    /// 파일 스냅샷에서 품질 기록 aggregate를 복원합니다.
    /// </summary>
    /// <returns>복원된 품질 기록 aggregate입니다.</returns>
    public QualityRecord Restore()
    {
        return QualityRecord.Restore(new QualityRecordRestoreState(
            new QualityRecordId(QualityRecordId),
            new WipUnitId(WipUnitId),
            InspectionCode,
            Status,
            DecisionStatus,
            HoldReason,
            DecisionNote));
    }
}

/// <summary>
/// 자재 요구 snapshot 파일 스냅샷입니다.
/// </summary>
/// <param name="OperationMaterialRequirementId">자재 요구 snapshot 식별자입니다.</param>
/// <param name="OperationExecutionId">연결된 공정 실행 식별자입니다.</param>
/// <param name="MaterialCode">자재 코드입니다.</param>
/// <param name="RequiredQuantity">요구 수량입니다.</param>
/// <param name="SequenceNo">요구 순번입니다.</param>
/// <param name="SourceRevisionRef">원본 revision 참조입니다.</param>
/// <param name="CreatedAt">snapshot 생성 시각입니다.</param>
internal sealed record OperationMaterialRequirementFileSnapshot(
    string OperationMaterialRequirementId,
    string OperationExecutionId,
    string MaterialCode,
    MeasuredQuantity RequiredQuantity,
    int SequenceNo,
    string? SourceRevisionRef,
    DateTimeOffset CreatedAt)
{
    /// <summary>
    /// 도메인 snapshot에서 파일 스냅샷을 생성합니다.
    /// </summary>
    /// <param name="snapshot">파일 스냅샷으로 변환할 도메인 snapshot입니다.</param>
    /// <returns>파일 저장용 자재 요구 스냅샷입니다.</returns>
    public static OperationMaterialRequirementFileSnapshot FromSnapshot(OperationMaterialRequirementSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new OperationMaterialRequirementFileSnapshot(
            snapshot.OperationMaterialRequirementId,
            snapshot.OperationExecutionId.ToString(),
            snapshot.MaterialCode,
            snapshot.RequiredQuantity,
            snapshot.Metadata.SequenceNo,
            snapshot.Metadata.SourceRevisionRef,
            snapshot.Metadata.CreatedAt);
    }

    /// <summary>
    /// 파일 스냅샷에서 도메인 snapshot을 복원합니다.
    /// </summary>
    /// <returns>복원된 자재 요구 snapshot입니다.</returns>
    public OperationMaterialRequirementSnapshot Restore()
    {
        return new OperationMaterialRequirementSnapshot(
            OperationMaterialRequirementId,
            new OperationExecutionId(OperationExecutionId),
            MaterialCode,
            RequiredQuantity,
            new OperationMaterialRequirementMetadata(
                SequenceNo,
                SourceRevisionRef,
                CreatedAt));
    }
}

/// <summary>
/// 파일 저장소 초기 시드 입력입니다.
/// </summary>
public sealed record FileOperatorExecutionSeed
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
    /// 초기 receipt 목록입니다.
    /// </summary>
    public IReadOnlyCollection<CommandReceiptRecord> Receipts { get; init; } = [];

    /// <summary>
    /// 초기 production actuals batch 목록입니다.
    /// </summary>
    public IReadOnlyCollection<PreparedProductionActualsBatch> ProductionActualsBatches { get; init; } = [];
}

/// <summary>
/// 파일 저장소 커밋 입력입니다.
/// </summary>
internal sealed record FileOperatorExecutionCommitRequest
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
    /// 저장할 receipt입니다.
    /// </summary>
    public CommandReceiptRecord? Receipt { get; init; }

    /// <summary>
    /// 저장할 production actuals batch입니다.
    /// </summary>
    public PreparedProductionActualsBatch? PreparedBatch { get; init; }

    /// <summary>
    /// 저장할 outbox 초안 목록입니다.
    /// </summary>
    public IReadOnlyCollection<FileOperatorExecutionOutboxDraft> OutboxEntries { get; init; } = [];

    /// <summary>
    /// 커밋 시각입니다.
    /// </summary>
    public DateTimeOffset CommittedAt { get; init; }
}

/// <summary>
/// 파일 저장용 outbox 초안입니다.
/// </summary>
/// <param name="AggregateType">원본 aggregate 형식입니다.</param>
/// <param name="AggregateId">원본 aggregate 식별자입니다.</param>
/// <param name="DomainEvent">저장할 domain event입니다.</param>
internal sealed record FileOperatorExecutionOutboxDraft(
    string AggregateType,
    string AggregateId,
    IDomainEvent DomainEvent);

/// <summary>
/// 파일 저장소에 기록된 outbox 항목입니다.
/// </summary>
/// <param name="OutboxEventId">outbox 식별자입니다.</param>
/// <param name="AggregateType">원본 aggregate 형식입니다.</param>
/// <param name="AggregateId">원본 aggregate 식별자입니다.</param>
/// <param name="EventType">domain event 형식 이름입니다.</param>
/// <param name="OccurredAt">domain event 발생 시각입니다.</param>
/// <param name="PayloadJson">직렬화된 domain event payload입니다.</param>
/// <param name="PersistedAt">outbox 저장 시각입니다.</param>
public sealed record FileOperatorExecutionOutboxEntry(
    string OutboxEventId,
    string AggregateType,
    string AggregateId,
    string EventType,
    DateTimeOffset OccurredAt,
    string PayloadJson,
    DateTimeOffset PersistedAt);
