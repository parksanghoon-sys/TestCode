using Mes.Application.Contracts.Common;
using Mes.Application.Contracts.OperatorExecution;
using Mes.Domain.ValueObjects;

namespace Mes.Application.OperatorExecution.WorkQueue;

/// <summary>
/// `GetStationWorkQueue` query를 application 계약 응답으로 조합합니다.
/// </summary>
public sealed class GetStationWorkQueueQueryHandler
{
    private readonly StationWorkQueueReadService _readService;

    /// <summary>
    /// 작업 큐 query handler를 초기화합니다.
    /// </summary>
    /// <param name="readService">MES-side 작업 큐 read service입니다.</param>
    public GetStationWorkQueueQueryHandler(StationWorkQueueReadService readService)
    {
        _readService = readService;
    }

    /// <summary>
    /// 작업 큐 query를 처리합니다.
    /// </summary>
    /// <param name="request">작업 큐 query 처리 입력입니다.</param>
    /// <returns>계약 응답 형식으로 변환된 작업 큐 snapshot입니다.</returns>
    public GetStationWorkQueueResponseContract Handle(GetStationWorkQueueHandlingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Request);
        ArgumentNullException.ThrowIfNull(request.Source);

        var snapshot = _readService.BuildSnapshot(
            new GetStationWorkQueueQueryRequest(
                new StationId(request.Request.StationId),
                request.SnapshotTakenAt,
                request.Source));

        return new GetStationWorkQueueResponseContract(
            snapshot.StationId.ToString(),
            snapshot.SnapshotTakenAt,
            snapshot.Items.Select(MapItem).ToList());
    }

    /// <summary>
    /// domain 작업 큐 항목을 transport 계약 항목으로 변환합니다.
    /// </summary>
    /// <param name="item">변환할 domain 작업 큐 항목입니다.</param>
    /// <returns>transport 계약 항목입니다.</returns>
    private static WorkQueueItemContract MapItem(StationWorkQueueItem item)
    {
        return new WorkQueueItemContract(
            item.Identity.ProductionOrderId.ToString(),
            item.Identity.OperationExecutionId.ToString(),
            item.Identity.OperationSequence,
            item.Identity.StationId.ToString(),
            item.State.OperationStatus.ToString(),
            item.State.OperationQuantityUnit,
            item.RequiredMaterials.Select(MapRequiredMaterial).ToList(),
            item.State.QualityGateState == StationWorkQueueQualityGateState.Hold
                ? QualityGateStateValues.Hold
                : QualityGateStateValues.Open);
    }

    /// <summary>
    /// domain 자재 요구 항목을 transport 계약 항목으로 변환합니다.
    /// </summary>
    /// <param name="material">변환할 자재 요구 항목입니다.</param>
    /// <returns>transport 계약 항목입니다.</returns>
    private static RequiredMaterialContract MapRequiredMaterial(StationWorkQueueRequiredMaterial material)
    {
        return new RequiredMaterialContract(
            material.MaterialCode,
            new MeasuredQuantityContract(material.RequiredQuantity.Value, material.RequiredQuantity.Unit));
    }
}
