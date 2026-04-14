using Mes.Domain.Abstractions;
using Mes.Domain.Common;
using Mes.Domain.Events;
using Mes.Domain.Statuses;
using Mes.Domain.ValueObjects;

namespace Mes.Domain.Aggregates;

/// <summary>
/// 예외 처리 또는 우회 실행에 대한 승인 요청을 관리하는 애그리거트입니다.
/// </summary>
public sealed class OverrideRequest : AggregateRoot<OverrideRequestId>
{
    /// <summary>
    /// 예외 승인 요청 애그리거트를 초기화합니다.
    /// </summary>
    /// <param name="id">예외 승인 요청 식별자입니다.</param>
    /// <param name="operationExecutionId">대상 공정 실행 식별자입니다.</param>
    /// <param name="requestedBy">요청자입니다.</param>
    /// <param name="reason">요청 사유입니다.</param>
    /// <param name="requestedAt">요청 시각입니다.</param>
    private OverrideRequest(
        OverrideRequestId id,
        OperationExecutionId operationExecutionId,
        string requestedBy,
        string reason,
        DateTimeOffset requestedAt) : base(id)
    {
        OperationExecutionId = operationExecutionId;
        RequestedBy = DomainGuard.NotWhiteSpace(requestedBy, nameof(requestedBy));
        Reason = DomainGuard.NotWhiteSpace(reason, nameof(reason));
        RequestedAt = requestedAt;
        Status = OverrideRequestStatus.Requested;
    }

    public OperationExecutionId OperationExecutionId { get; }

    public string RequestedBy { get; }

    public string Reason { get; }

    public DateTimeOffset RequestedAt { get; }

    public OverrideRequestStatus Status { get; private set; }

    public string? ReviewedBy { get; private set; }

    public string? ReviewNote { get; private set; }

    public DateTimeOffset? ReviewedAt { get; private set; }

    /// <summary>
    /// 새로운 예외 승인 요청을 생성하고 요청 이벤트를 발생시킵니다.
    /// </summary>
    /// <param name="id">예외 승인 요청 식별자입니다.</param>
    /// <param name="operationExecutionId">대상 공정 실행 식별자입니다.</param>
    /// <param name="requestedBy">요청자입니다.</param>
    /// <param name="reason">요청 사유입니다.</param>
    /// <param name="requestedAt">요청 시각입니다.</param>
    /// <returns>요청 상태의 예외 승인 요청입니다.</returns>
    public static OverrideRequest Create(
        OverrideRequestId id,
        OperationExecutionId operationExecutionId,
        string requestedBy,
        string reason,
        DateTimeOffset requestedAt)
    {
        var request = new OverrideRequest(id, operationExecutionId, requestedBy, reason, requestedAt);
        request.Raise(new OverrideRequestedDomainEvent(request.Id, request.OperationExecutionId, request.RequestedBy, requestedAt));
        return request;
    }

    /// <summary>
    /// 대기 중인 예외 승인 요청을 승인합니다.
    /// </summary>
    /// <param name="approvedBy">승인자입니다.</param>
    /// <param name="reviewNote">검토 메모입니다.</param>
    /// <param name="reviewedAt">검토 시각입니다.</param>
    public void Approve(string approvedBy, string reviewNote, DateTimeOffset reviewedAt)
    {
        EnsurePending();

        ReviewedBy = DomainGuard.NotWhiteSpace(approvedBy, nameof(approvedBy));
        ReviewNote = DomainGuard.NotWhiteSpace(reviewNote, nameof(reviewNote));
        ReviewedAt = reviewedAt;
        Status = OverrideRequestStatus.Approved;

        Raise(new OverrideApprovedDomainEvent(Id, ReviewedBy, reviewedAt));
    }

    /// <summary>
    /// 대기 중인 예외 승인 요청을 반려합니다.
    /// </summary>
    /// <param name="rejectedBy">반려자입니다.</param>
    /// <param name="reviewNote">검토 메모입니다.</param>
    /// <param name="reviewedAt">검토 시각입니다.</param>
    public void Reject(string rejectedBy, string reviewNote, DateTimeOffset reviewedAt)
    {
        EnsurePending();

        ReviewedBy = DomainGuard.NotWhiteSpace(rejectedBy, nameof(rejectedBy));
        ReviewNote = DomainGuard.NotWhiteSpace(reviewNote, nameof(reviewNote));
        ReviewedAt = reviewedAt;
        Status = OverrideRequestStatus.Rejected;

        Raise(new OverrideRejectedDomainEvent(Id, ReviewedBy, reviewedAt));
    }

    /// <summary>
    /// 현재 요청이 검토 가능한 대기 상태인지 확인합니다.
    /// </summary>
    private void EnsurePending()
    {
        DomainGuard.Against(Status != OverrideRequestStatus.Requested, "Only a requested override can be reviewed.");
    }
}
