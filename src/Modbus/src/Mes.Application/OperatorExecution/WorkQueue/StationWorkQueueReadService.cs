using Mes.Domain.Aggregates;
using Mes.Domain.Entities;
using Mes.Domain.Statuses;
using Mes.Domain.ValueObjects;

namespace Mes.Application.OperatorExecution.WorkQueue;

/// <summary>
/// MES-side snapshot source만 사용해 스테이션 작업 큐를 구성합니다.
/// </summary>
public sealed class StationWorkQueueReadService
{
    /// <summary>
    /// 스테이션 작업 큐 snapshot을 구성합니다.
    /// </summary>
    /// <param name="request">작업 큐 조회 요청입니다.</param>
    /// <returns>MES-side source만으로 구성한 작업 큐 snapshot입니다.</returns>
    public StationWorkQueueSnapshot BuildSnapshot(GetStationWorkQueueQueryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Source);

        var heldQualityOperations = GetHeldQualityOperationIds(request.Source);
        var requiredMaterialsByOperation = GetRequiredMaterialsByOperation(request.Source.MaterialRequirements);

        var items = request.Source.OperationExecutions
            .Where(operation => operation.StationId is not null && operation.StationId.Value.Equals(request.StationId))
            .OrderBy(operation => operation.OperationSequence)
            .ThenBy(operation => operation.Id.ToString(), StringComparer.Ordinal)
            .Select(operation => CreateItem(
                operation,
                operation.StationId!.Value,
                heldQualityOperations,
                requiredMaterialsByOperation))
            .ToList();

        return new StationWorkQueueSnapshot(
            request.StationId,
            request.SnapshotTakenAt,
            items);
    }

    /// <summary>
    /// 품질 hold가 현재 영향을 주는 공정 실행 식별자 집합을 계산합니다.
    /// </summary>
    /// <param name="source">작업 큐 조회 source입니다.</param>
    /// <returns>품질 hold 영향이 있는 공정 실행 식별자 집합입니다.</returns>
    private static ISet<OperationExecutionId> GetHeldQualityOperationIds(StationWorkQueueSource source)
    {
        var heldQualityWips = source.QualityRecords
            .Where(record => record.Status == QualityRecordStatus.Hold)
            .Select(record => record.WipUnitId)
            .ToHashSet();

        return source.WipUnits
            .Where(unit => unit.CurrentOperationExecutionId is not null && heldQualityWips.Contains(unit.Id))
            .Select(unit => unit.CurrentOperationExecutionId!.Value)
            .ToHashSet();
    }

    /// <summary>
    /// 공정 실행별 자재 요구량 snapshot을 정렬된 형태로 묶습니다.
    /// </summary>
    /// <param name="snapshots">원본 snapshot 목록입니다.</param>
    /// <returns>공정 실행 식별자별 요구량 목록입니다.</returns>
    private static IReadOnlyDictionary<OperationExecutionId, IReadOnlyList<StationWorkQueueRequiredMaterial>> GetRequiredMaterialsByOperation(
        IEnumerable<OperationMaterialRequirementSnapshot> snapshots)
    {
        return snapshots
            .GroupBy(snapshot => snapshot.OperationExecutionId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<StationWorkQueueRequiredMaterial>)group
                    .OrderBy(snapshot => snapshot.Metadata.SequenceNo)
                    .Select(snapshot => new StationWorkQueueRequiredMaterial(
                        snapshot.MaterialCode,
                        snapshot.RequiredQuantity,
                        snapshot.Metadata.SequenceNo))
                    .ToList());
    }

    /// <summary>
    /// 단일 공정 실행에 대한 작업 큐 항목을 생성합니다.
    /// </summary>
    /// <param name="operation">대상 공정 실행입니다.</param>
    /// <param name="heldQualityWips">품질 hold가 걸린 WIP 식별자 집합입니다.</param>
    /// <param name="requiredMaterialsByOperation">공정 실행별 자재 요구량 lookup입니다.</param>
    /// <returns>구성된 작업 큐 항목입니다.</returns>
    private static StationWorkQueueItem CreateItem(
        OperationExecution operation,
        StationId stationId,
        ISet<OperationExecutionId> heldQualityOperations,
        IReadOnlyDictionary<OperationExecutionId, IReadOnlyList<StationWorkQueueRequiredMaterial>> requiredMaterialsByOperation)
    {
        var qualityGateState = GetQualityGateState(operation, heldQualityOperations);
        var requiredMaterials = requiredMaterialsByOperation.TryGetValue(operation.Id, out var materials)
            ? materials
            : [];

        return new StationWorkQueueItem(
            new StationWorkQueueIdentity(
                operation.ProductionOrderId,
                operation.Id,
                operation.OperationSequence,
                stationId),
            new StationWorkQueueState(
                operation.Status,
                qualityGateState,
                operation.QuantityUnit),
            requiredMaterials);
    }

    /// <summary>
    /// 공정 실행의 품질 게이트 상태를 계산합니다.
    /// </summary>
    /// <param name="operation">대상 공정 실행입니다.</param>
    /// <param name="heldQualityWips">품질 hold가 걸린 WIP 식별자 집합입니다.</param>
    /// <returns>Release 1 규칙에 따른 품질 게이트 상태입니다.</returns>
    private static StationWorkQueueQualityGateState GetQualityGateState(
        OperationExecution operation,
        ISet<OperationExecutionId> heldQualityOperations)
    {
        if (operation.Status == OperationExecutionStatus.Hold)
        {
            return StationWorkQueueQualityGateState.Hold;
        }

        return heldQualityOperations.Contains(operation.Id)
            ? StationWorkQueueQualityGateState.Hold
            : StationWorkQueueQualityGateState.Open;
    }
}
