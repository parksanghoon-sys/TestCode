using Mes.Application.Contracts.OperatorExecution;
using Mes.Domain.Abstractions;
using Mes.Domain.Aggregates;
using Mes.Domain.ValueObjects;

namespace Mes.Application.OperatorExecution;

/// <summary>
/// 공정 완료 이후 ERP posting 대기용 production actuals batch skeleton 을 준비합니다.
/// </summary>
public sealed class ProductionActualsPreparationService
{
    /// <summary>
    /// 완료된 공정 실행으로부터 production actuals batch skeleton 을 준비합니다.
    /// </summary>
    /// <param name="request">준비 대상 aggregate 와 시각 정보입니다.</param>
    /// <returns>pending-projection 상태의 actuals batch skeleton 입니다.</returns>
    public PreparedProductionActualsBatch PrepareForCompletedOperation(PrepareProductionActualsBatchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.ProductionOrder);
        ArgumentNullException.ThrowIfNull(request.OperationExecution);

        if (request.OperationExecution.Status != Mes.Domain.Statuses.OperationExecutionStatus.Done)
        {
            throw new DomainException("Production actuals can only be prepared after the operation is completed.");
        }

        if (request.OperationExecution.CompletedAt is null)
        {
            throw new DomainException("Completed operations must keep a completion timestamp before production actuals are prepared.");
        }

        if (request.OperationExecution.ProductionOrderId != request.ProductionOrder.Id)
        {
            throw new DomainException("Production actuals preparation requires a matching production order and operation execution.");
        }

        return new PreparedProductionActualsBatch(
            CreateActualsBatchId(request.ProductionOrder.Id, request.OperationExecution.Id),
            request.ProductionOrder.Id,
            request.OperationExecution.Id,
            request.OperationExecution.GoodQuantity,
            request.OperationExecution.ScrapQuantity,
            ProductionActualsStatusValues.PendingProjection,
            request.PreparedAt);
    }

    /// <summary>
    /// release-1 batch skeleton 에 사용할 결정적 actuals batch 식별자를 생성합니다.
    /// </summary>
    /// <param name="productionOrderId">상위 생산 오더 식별자입니다.</param>
    /// <param name="operationExecutionId">완료된 공정 실행 식별자입니다.</param>
    /// <returns>결정적 actuals batch 식별자입니다.</returns>
    private static string CreateActualsBatchId(ProductionOrderId productionOrderId, OperationExecutionId operationExecutionId)
    {
        return $"ACT-{productionOrderId}-{operationExecutionId}";
    }
}

/// <summary>
/// production actuals batch skeleton 준비 입력 묶음입니다.
/// </summary>
/// <param name="ProductionOrder">상위 생산 오더입니다.</param>
/// <param name="OperationExecution">완료된 공정 실행입니다.</param>
/// <param name="PreparedAt">batch 준비 시각입니다.</param>
public sealed record PrepareProductionActualsBatchRequest(
    ProductionOrder ProductionOrder,
    OperationExecution OperationExecution,
    DateTimeOffset PreparedAt);

/// <summary>
/// ERP posting 대기용 production actuals batch skeleton 입니다.
/// </summary>
/// <param name="ActualsBatchId">batch 식별자입니다.</param>
/// <param name="ProductionOrderId">상위 생산 오더 식별자입니다.</param>
/// <param name="OperationExecutionId">완료된 공정 실행 식별자입니다.</param>
/// <param name="GoodQuantity">양품 수량입니다.</param>
/// <param name="ScrapQuantity">불량 수량입니다.</param>
/// <param name="Status">현재 batch 상태입니다.</param>
/// <param name="PreparedAt">batch 준비 시각입니다.</param>
public sealed record PreparedProductionActualsBatch(
    string ActualsBatchId,
    ProductionOrderId ProductionOrderId,
    OperationExecutionId OperationExecutionId,
    MeasuredQuantity GoodQuantity,
    MeasuredQuantity ScrapQuantity,
    string Status,
    DateTimeOffset PreparedAt);
