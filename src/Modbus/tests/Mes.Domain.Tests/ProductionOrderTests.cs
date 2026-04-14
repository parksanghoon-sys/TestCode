using Mes.Domain.Aggregates;
using Mes.Domain.Events;
using Mes.Domain.Statuses;
using Mes.Domain.ValueObjects;

namespace Mes.Domain.Tests;

/// <summary>
/// 생산 오더 애그리거트의 상태 전이를 검증합니다.
/// </summary>
public class ProductionOrderTests
{
    /// <summary>
    /// 생산 오더 릴리즈 후 공정을 연결하면 상태가 배차됨으로 전환되는지 검증합니다.
    /// </summary>
    [Fact]
    public void Release_and_attach_operation_should_move_order_to_dispatched()
    {
        var releasedAt = new DateTimeOffset(2026, 4, 14, 10, 0, 0, TimeSpan.Zero);
        var order = ProductionOrder.Release(new ProductionOrderId("PO-1001"), "ITEM-01", "ROUTE-A", releasedAt);

        order.AttachOperation(new OperationExecutionId("OP-10"));

        Assert.Equal(ProductionOrderStatus.Dispatched, order.Status);
        Assert.Contains(order.DomainEvents, e => e is OrderReleasedIngestedDomainEvent);
    }
}
