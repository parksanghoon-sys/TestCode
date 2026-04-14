using Mes.Domain.Abstractions;
using Mes.Domain.Aggregates;
using Mes.Domain.Events;
using Mes.Domain.Statuses;
using Mes.Domain.ValueObjects;

namespace Mes.Domain.Tests;

/// <summary>
/// 예외 승인 요청 애그리거트의 검토 규칙을 검증합니다.
/// </summary>
public class OverrideRequestTests
{
    /// <summary>
    /// 승인된 예외 요청은 다시 반려할 수 없는지 검증합니다.
    /// </summary>
    [Fact]
    public void Approved_override_should_not_be_rejected_again()
    {
        var request = OverrideRequest.Create(
            new OverrideRequestId("OVR-1001"),
            new OperationExecutionId("OP-2001"),
            "operator-01",
            "missing scanner fallback",
            DateTimeOffset.UtcNow);

        request.Approve("supervisor-01", "approved for pilot", DateTimeOffset.UtcNow);

        Assert.Equal(OverrideRequestStatus.Approved, request.Status);
        Assert.Throws<DomainException>(() => request.Reject("quality-01", "too late", DateTimeOffset.UtcNow));
        Assert.Contains(request.DomainEvents, e => e is OverrideRequestedDomainEvent);
        Assert.Contains(request.DomainEvents, e => e is OverrideApprovedDomainEvent);
    }
}
