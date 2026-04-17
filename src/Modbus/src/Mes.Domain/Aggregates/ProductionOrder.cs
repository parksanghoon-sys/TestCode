using Mes.Domain.Abstractions;
using Mes.Domain.Common;
using Mes.Domain.Events;
using Mes.Domain.Statuses;
using Mes.Domain.ValueObjects;

namespace Mes.Domain.Aggregates;

/// <summary>
/// MES에서 실행 오더의 수명주기를 관리하는 생산 오더 애그리거트입니다.
/// </summary>
public sealed class ProductionOrder : AggregateRoot<ProductionOrderId>
{
    private readonly List<OperationExecutionId> _operationIds = [];

    /// <summary>
    /// 생산 오더를 초기화합니다.
    /// </summary>
    /// <param name="id">생산 오더 식별자입니다.</param>
    /// <param name="itemCode">생산 대상 품목 코드입니다.</param>
    /// <param name="routeRevision">적용 라우팅 리비전입니다.</param>
    /// <param name="releasedAt">오더 릴리즈 시각입니다.</param>
    private ProductionOrder(
        ProductionOrderId id,
        string itemCode,
        string routeRevision,
        DateTimeOffset releasedAt) : base(id)
    {
        ItemCode = DomainGuard.NotWhiteSpace(itemCode, nameof(itemCode));
        RouteRevision = DomainGuard.NotWhiteSpace(routeRevision, nameof(routeRevision));
        ReleasedAt = releasedAt;
        Status = ProductionOrderStatus.Released;
    }

    public string ItemCode { get; }

    public string RouteRevision { get; }

    public ProductionOrderStatus Status { get; private set; }

    public DateTimeOffset ReleasedAt { get; }

    public IReadOnlyCollection<OperationExecutionId> OperationIds => _operationIds.AsReadOnly();

    /// <summary>
    /// 릴리즈된 생산 오더를 생성하고 릴리즈 도메인 이벤트를 발생시킵니다.
    /// </summary>
    /// <param name="id">생산 오더 식별자입니다.</param>
    /// <param name="itemCode">생산 대상 품목 코드입니다.</param>
    /// <param name="routeRevision">적용 라우팅 리비전입니다.</param>
    /// <param name="releasedAt">오더 릴리즈 시각입니다.</param>
    /// <returns>릴리즈 상태의 생산 오더입니다.</returns>
    public static ProductionOrder Release(
        ProductionOrderId id,
        string itemCode,
        string routeRevision,
        DateTimeOffset releasedAt)
    {
        var order = new ProductionOrder(id, itemCode, routeRevision, releasedAt);
        order.Raise(new OrderReleasedIngestedDomainEvent(order.Id, order.ItemCode, order.RouteRevision, releasedAt));
        return order;
    }

    /// <summary>
    /// 영속 스냅샷에서 생산 오더 aggregate를 복원합니다.
    /// </summary>
    /// <param name="state">복원할 생산 오더 상태입니다.</param>
    /// <returns>도메인 이벤트가 비어 있는 생산 오더 aggregate입니다.</returns>
    public static ProductionOrder Restore(ProductionOrderRestoreState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var order = new ProductionOrder(
            state.Id,
            state.ItemCode,
            state.RouteRevision,
            state.ReleasedAt)
        {
            Status = state.Status
        };

        order._operationIds.AddRange(state.OperationIds);
        return order;
    }

    /// <summary>
    /// 생산 오더에 실행 공정을 연결합니다.
    /// </summary>
    /// <param name="operationExecutionId">연결할 공정 실행 식별자입니다.</param>
    public void AttachOperation(OperationExecutionId operationExecutionId)
    {
        DomainGuard.Against(Status is ProductionOrderStatus.Closed or ProductionOrderStatus.Cancelled, "Cannot attach operations to a closed or cancelled order.");
        DomainGuard.Against(_operationIds.Contains(operationExecutionId), "Operation is already attached to the order.");

        _operationIds.Add(operationExecutionId);

        if (Status == ProductionOrderStatus.Released)
        {
            Status = ProductionOrderStatus.Dispatched;
        }
    }

    /// <summary>
    /// 생산 오더를 작업 진행 중 상태로 전환합니다.
    /// </summary>
    public void MarkInProgress()
    {
        DomainGuard.Against(Status is not (ProductionOrderStatus.Dispatched or ProductionOrderStatus.PartiallyCompleted), "Order must be dispatched before it can start.");
        Status = ProductionOrderStatus.InProgress;
    }

    /// <summary>
    /// 생산 오더를 부분 완료 상태로 전환합니다.
    /// </summary>
    public void MarkPartiallyCompleted()
    {
        DomainGuard.Against(Status != ProductionOrderStatus.InProgress, "Only an in-progress order can be partially completed.");
        Status = ProductionOrderStatus.PartiallyCompleted;
    }

    /// <summary>
    /// 생산 오더를 완료 상태로 전환합니다.
    /// </summary>
    public void MarkCompleted()
    {
        DomainGuard.Against(Status is not (ProductionOrderStatus.Dispatched or ProductionOrderStatus.InProgress or ProductionOrderStatus.PartiallyCompleted), "Order cannot be completed from its current state.");
        DomainGuard.Against(_operationIds.Count == 0, "Order must contain at least one operation before completion.");
        Status = ProductionOrderStatus.Completed;
    }

    /// <summary>
    /// 완료된 생산 오더를 종료 상태로 전환합니다.
    /// </summary>
    public void Close()
    {
        DomainGuard.Against(Status != ProductionOrderStatus.Completed, "Only a completed order can be closed.");
        Status = ProductionOrderStatus.Closed;
    }

    /// <summary>
    /// 생산 오더를 취소 상태로 전환합니다.
    /// </summary>
    public void Cancel()
    {
        DomainGuard.Against(Status == ProductionOrderStatus.Closed, "A closed order cannot be cancelled.");
        Status = ProductionOrderStatus.Cancelled;
    }
}

/// <summary>
/// 생산 오더 aggregate 복원에 필요한 상태 묶음입니다.
/// </summary>
/// <param name="Id">생산 오더 식별자입니다.</param>
/// <param name="ItemCode">생산 대상 품목 코드입니다.</param>
/// <param name="RouteRevision">적용된 라우팅 리비전입니다.</param>
/// <param name="Status">현재 생산 오더 상태입니다.</param>
/// <param name="ReleasedAt">오더 릴리즈 시각입니다.</param>
/// <param name="OperationIds">오더에 연결된 공정 실행 식별자 목록입니다.</param>
public sealed record ProductionOrderRestoreState(
    ProductionOrderId Id,
    string ItemCode,
    string RouteRevision,
    ProductionOrderStatus Status,
    DateTimeOffset ReleasedAt,
    IReadOnlyCollection<OperationExecutionId> OperationIds);
