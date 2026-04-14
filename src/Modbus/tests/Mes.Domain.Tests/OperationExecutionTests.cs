using Mes.Domain.Abstractions;
using Mes.Domain.Aggregates;
using Mes.Domain.Events;
using Mes.Domain.Statuses;
using Mes.Domain.ValueObjects;

namespace Mes.Domain.Tests;

/// <summary>
/// 공정 실행 애그리거트의 핵심 상태 전이와 이벤트 발생을 검증합니다.
/// </summary>
public class OperationExecutionTests
{
    /// <summary>
    /// 공정 시작 시 실행 상태와 시작 이벤트가 올바르게 반영되는지 검증합니다.
    /// </summary>
    [Fact]
    public void Start_should_raise_event_and_change_status()
    {
        var operation = OperationExecution.Create(
            new OperationExecutionId("OP-1001"),
            new ProductionOrderId("PO-1001"),
            10);

        operation.QueueForExecution();
        operation.Start(new StationId("ST-01"), DateTimeOffset.UtcNow);

        Assert.Equal(OperationExecutionStatus.Running, operation.Status);
        Assert.Contains(operation.DomainEvents, e => e is OperationStartedDomainEvent);
    }

    /// <summary>
    /// Hold 상태의 공정은 해제되기 전까지 완료할 수 없는지 검증합니다.
    /// </summary>
    [Fact]
    public void Held_operation_should_not_complete_until_release()
    {
        var operation = OperationExecution.Create(
            new OperationExecutionId("OP-1002"),
            new ProductionOrderId("PO-1002"),
            20);

        operation.QueueForExecution();
        operation.Start(new StationId("ST-02"), DateTimeOffset.UtcNow);
        operation.PlaceHold("quality gate", DateTimeOffset.UtcNow);

        Assert.Throws<DomainException>(() => operation.Complete(new MeasuredQuantity(1, "EA"), DateTimeOffset.UtcNow));

        operation.ReleaseHold("quality cleared", DateTimeOffset.UtcNow);
        operation.Complete(new MeasuredQuantity(1, "EA"), DateTimeOffset.UtcNow);

        Assert.Equal(OperationExecutionStatus.Done, operation.Status);
        Assert.Contains(operation.DomainEvents, e => e is HoldPlacedDomainEvent);
        Assert.Contains(operation.DomainEvents, e => e is HoldReleasedDomainEvent);
        Assert.Contains(operation.DomainEvents, e => e is OperationCompletedDomainEvent);
    }
}
