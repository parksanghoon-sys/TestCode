using Mes.Domain.Common;
using Mes.Domain.Statuses;
using Mes.Domain.ValueObjects;

namespace Mes.Domain.Entities;

/// <summary>
/// 공정 사이를 이동하는 WIP 단위를 표현합니다.
/// </summary>
public sealed class WipUnit
{
    private WipUnitStatus? _statusBeforeHold;

    /// <summary>
    /// WIP 단위를 초기화합니다.
    /// </summary>
    /// <param name="id">WIP 단위 식별자입니다.</param>
    /// <param name="productCode">생산 품목 코드입니다.</param>
    public WipUnit(WipUnitId id, string productCode)
    {
        Id = id;
        ProductCode = DomainGuard.NotWhiteSpace(productCode, nameof(productCode));
        Status = WipUnitStatus.Queued;
    }

    /// <summary>
    /// 영속 스냅샷 복원에 사용하는 내부 생성자입니다.
    /// </summary>
    /// <param name="id">WIP 식별자입니다.</param>
    /// <param name="productCode">생산 품목 코드입니다.</param>
    /// <param name="status">현재 WIP 상태입니다.</param>
    /// <param name="currentOperationExecutionId">현재 연결된 공정 실행 식별자입니다.</param>
    /// <param name="holdReason">현재 hold 사유입니다.</param>
    /// <param name="statusBeforeHold">hold 직전 WIP 상태입니다.</param>
    private WipUnit(
        WipUnitId id,
        string productCode,
        WipUnitStatus status,
        OperationExecutionId? currentOperationExecutionId,
        string? holdReason,
        WipUnitStatus? statusBeforeHold)
    {
        Id = id;
        ProductCode = DomainGuard.NotWhiteSpace(productCode, nameof(productCode));
        Status = status;
        CurrentOperationExecutionId = currentOperationExecutionId;
        HoldReason = string.IsNullOrWhiteSpace(holdReason)
            ? null
            : holdReason.Trim();
        _statusBeforeHold = statusBeforeHold;
    }

    public WipUnitId Id { get; }

    public string ProductCode { get; }

    public WipUnitStatus Status { get; private set; }

    public OperationExecutionId? CurrentOperationExecutionId { get; private set; }

    public string? HoldReason { get; private set; }

    /// <summary>
    /// hold 직전 WIP 상태입니다.
    /// </summary>
    public WipUnitStatus? StatusBeforeHold => _statusBeforeHold;

    /// <summary>
    /// 영속 스냅샷에서 WIP 엔티티를 복원합니다.
    /// </summary>
    /// <param name="state">복원할 WIP 상태입니다.</param>
    /// <returns>도메인 이벤트가 비어 있는 WIP 엔티티입니다.</returns>
    public static WipUnit Restore(WipUnitRestoreState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return new WipUnit(
            state.Id,
            state.ProductCode,
            state.Status,
            state.CurrentOperationExecutionId,
            state.HoldReason,
            state.StatusBeforeHold);
    }

    /// <summary>
    /// 지정한 공정 실행 대기 상태로 WIP를 배치합니다.
    /// </summary>
    /// <param name="operationExecutionId">대기시킬 공정 실행 식별자입니다.</param>
    public void QueueFor(OperationExecutionId operationExecutionId)
    {
        CurrentOperationExecutionId = operationExecutionId;
        Status = WipUnitStatus.Queued;
    }

    /// <summary>
    /// WIP를 지정한 공정 실행에서 처리 중 상태로 전환합니다.
    /// </summary>
    /// <param name="operationExecutionId">현재 처리 중인 공정 실행 식별자입니다.</param>
    public void StartProcessing(OperationExecutionId operationExecutionId)
    {
        CurrentOperationExecutionId = operationExecutionId;
        Status = WipUnitStatus.InProcess;
    }

    /// <summary>
    /// WIP를 Hold 상태로 전환합니다.
    /// </summary>
    /// <param name="reason">Hold 사유입니다.</param>
    public void PlaceHold(string reason)
    {
        DomainGuard.Against(Status is WipUnitStatus.Scrapped or WipUnitStatus.Completed or WipUnitStatus.Hold, "WIP unit cannot be held from its current state.");
        HoldReason = DomainGuard.NotWhiteSpace(reason, nameof(reason));
        _statusBeforeHold = Status;
        Status = WipUnitStatus.Hold;
    }

    /// <summary>
    /// Hold 상태의 WIP를 이전 상태로 복귀시킵니다.
    /// </summary>
    public void ReleaseHold()
    {
        DomainGuard.Against(Status != WipUnitStatus.Hold, "WIP unit is not on hold.");
        HoldReason = null;
        Status = _statusBeforeHold ?? WipUnitStatus.Queued;
        _statusBeforeHold = null;
    }

    /// <summary>
    /// WIP를 재작업 상태로 전환합니다.
    /// </summary>
    public void MoveToRework()
    {
        Status = WipUnitStatus.Rework;
    }

    /// <summary>
    /// WIP를 폐기 상태로 전환합니다.
    /// </summary>
    public void Scrap()
    {
        Status = WipUnitStatus.Scrapped;
    }

    /// <summary>
    /// WIP를 완료 상태로 전환합니다.
    /// </summary>
    public void Complete()
    {
        Status = WipUnitStatus.Completed;
    }
}

/// <summary>
/// WIP 엔티티 복원에 필요한 상태 묶음입니다.
/// </summary>
/// <param name="Id">WIP 식별자입니다.</param>
/// <param name="ProductCode">생산 품목 코드입니다.</param>
/// <param name="Status">현재 WIP 상태입니다.</param>
/// <param name="CurrentOperationExecutionId">현재 연결된 공정 실행 식별자입니다.</param>
/// <param name="HoldReason">현재 hold 사유입니다.</param>
/// <param name="StatusBeforeHold">hold 직전 WIP 상태입니다.</param>
public sealed record WipUnitRestoreState(
    WipUnitId Id,
    string ProductCode,
    WipUnitStatus Status,
    OperationExecutionId? CurrentOperationExecutionId,
    string? HoldReason,
    WipUnitStatus? StatusBeforeHold);
