using Mes.Domain.Abstractions;
using Mes.Domain.Common;
using Mes.Domain.Events;
using Mes.Domain.Statuses;
using Mes.Domain.ValueObjects;

namespace Mes.Domain.Aggregates;

/// <summary>
/// 현장 투입 자재 Lot의 이동과 genealogy 연결을 관리하는 애그리거트입니다.
/// </summary>
public sealed class MaterialLot : AggregateRoot<MaterialLotId>
{
    private readonly List<GenealogyLink> _genealogyLinks = [];

    /// <summary>
    /// 자재 Lot 애그리거트를 초기화합니다.
    /// </summary>
    /// <param name="id">자재 Lot 식별자입니다.</param>
    /// <param name="materialCode">자재 코드입니다.</param>
    /// <param name="initialQuantity">초기 가용 수량입니다.</param>
    private MaterialLot(
        MaterialLotId id,
        string materialCode,
        MeasuredQuantity initialQuantity) : base(id)
    {
        MaterialCode = DomainGuard.NotWhiteSpace(materialCode, nameof(materialCode));
        AvailableQuantity = initialQuantity;
        ConsumedQuantity = MeasuredQuantity.Zero(initialQuantity.Unit);
        ReturnedQuantity = MeasuredQuantity.Zero(initialQuantity.Unit);
        Status = MaterialLotStatus.Available;
    }

    public string MaterialCode { get; }

    public MaterialLotStatus Status { get; private set; }

    public MeasuredQuantity AvailableQuantity { get; private set; }

    public MeasuredQuantity ConsumedQuantity { get; private set; }

    public MeasuredQuantity ReturnedQuantity { get; private set; }

    public string? BlockReason { get; private set; }

    public IReadOnlyCollection<GenealogyLink> GenealogyLinks => _genealogyLinks.AsReadOnly();

    /// <summary>
    /// 새로운 자재 Lot를 생성합니다.
    /// </summary>
    /// <param name="id">자재 Lot 식별자입니다.</param>
    /// <param name="materialCode">자재 코드입니다.</param>
    /// <param name="initialQuantity">초기 가용 수량입니다.</param>
    /// <returns>초기 상태의 자재 Lot입니다.</returns>
    public static MaterialLot Create(
        MaterialLotId id,
        string materialCode,
        MeasuredQuantity initialQuantity)
    {
        return new MaterialLot(id, materialCode, initialQuantity);
    }

    /// <summary>
    /// 영속 스냅샷에서 자재 lot aggregate를 복원합니다.
    /// </summary>
    /// <param name="state">복원할 자재 lot 상태입니다.</param>
    /// <returns>도메인 이벤트가 비어 있는 자재 lot aggregate입니다.</returns>
    public static MaterialLot Restore(MaterialLotRestoreState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var lot = new MaterialLot(
            state.Id,
            state.MaterialCode,
            state.AvailableQuantity)
        {
            Status = state.Status,
            AvailableQuantity = state.AvailableQuantity,
            ConsumedQuantity = state.ConsumedQuantity,
            ReturnedQuantity = state.ReturnedQuantity,
            BlockReason = string.IsNullOrWhiteSpace(state.BlockReason)
                ? null
                : state.BlockReason.Trim()
        };

        lot._genealogyLinks.AddRange(state.GenealogyLinks);
        return lot;
    }

    /// <summary>
    /// 자재 Lot를 라인 투입 상태로 전환합니다.
    /// </summary>
    public void IssueToLine()
    {
        DomainGuard.Against(Status is MaterialLotStatus.Blocked or MaterialLotStatus.Consumed or MaterialLotStatus.Returned, "Material lot cannot be issued from its current state.");
        Status = MaterialLotStatus.Issued;
    }

    /// <summary>
    /// 자재 Lot를 차단 상태로 전환합니다.
    /// </summary>
    /// <param name="reason">차단 사유입니다.</param>
    public void Block(string reason)
    {
        BlockReason = DomainGuard.NotWhiteSpace(reason, nameof(reason));
        Status = MaterialLotStatus.Blocked;
    }

    /// <summary>
    /// 지정한 WIP 단위에 자재를 소모 처리합니다.
    /// </summary>
    /// <param name="wipUnitId">자재를 소모한 WIP 단위 식별자입니다.</param>
    /// <param name="quantity">소모 수량입니다.</param>
    /// <param name="occurredAt">소모 시각입니다.</param>
    public void ConsumeFor(WipUnitId wipUnitId, MeasuredQuantity quantity, DateTimeOffset occurredAt)
    {
        EnsureMovable(quantity);

        AvailableQuantity = AvailableQuantity.Subtract(quantity);
        ConsumedQuantity = ConsumedQuantity.Add(quantity);

        var genealogyLink = new GenealogyLink(Id, wipUnitId, occurredAt);
        _genealogyLinks.Add(genealogyLink);

        Status = AvailableQuantity.Value == 0m ? MaterialLotStatus.Consumed : MaterialLotStatus.Issued;

        Raise(new MaterialConsumptionRecordedDomainEvent(Id, wipUnitId, quantity, occurredAt));
        Raise(new GenealogyLinkCreatedDomainEvent(Id, wipUnitId, occurredAt));
    }

    /// <summary>
    /// 남은 자재를 창고로 반납 처리합니다.
    /// </summary>
    /// <param name="quantity">반납 수량입니다.</param>
    /// <param name="occurredAt">반납 시각입니다.</param>
    public void ReturnToWarehouse(MeasuredQuantity quantity, DateTimeOffset occurredAt)
    {
        EnsureMovable(quantity);

        AvailableQuantity = AvailableQuantity.Subtract(quantity);
        ReturnedQuantity = ReturnedQuantity.Add(quantity);

        Status = AvailableQuantity.Value == 0m ? MaterialLotStatus.Returned : MaterialLotStatus.Issued;

        Raise(new MaterialReturnRecordedDomainEvent(Id, quantity, occurredAt));
    }

    /// <summary>
    /// 현재 자재 Lot가 이동 가능한 상태인지 확인합니다.
    /// </summary>
    /// <param name="quantity">검증할 이동 수량입니다.</param>
    private void EnsureMovable(MeasuredQuantity quantity)
    {
        DomainGuard.Against(Status == MaterialLotStatus.Blocked, "Blocked material cannot move.");
        DomainGuard.Against(Status is MaterialLotStatus.Consumed or MaterialLotStatus.Returned, "Depleted material lot cannot move.");
        DomainGuard.Against(!string.Equals(AvailableQuantity.Unit, quantity.Unit, StringComparison.OrdinalIgnoreCase), "Quantity unit does not match the material lot unit.");
        DomainGuard.Against(quantity.Value > AvailableQuantity.Value, "Movement quantity cannot exceed available quantity.");
    }
}

/// <summary>
/// 자재 lot aggregate 복원에 필요한 상태 묶음입니다.
/// </summary>
/// <param name="Id">자재 lot 식별자입니다.</param>
/// <param name="MaterialCode">자재 코드입니다.</param>
/// <param name="Status">현재 자재 lot 상태입니다.</param>
/// <param name="AvailableQuantity">현재 가용 수량입니다.</param>
/// <param name="ConsumedQuantity">누적 소모 수량입니다.</param>
/// <param name="ReturnedQuantity">누적 반납 수량입니다.</param>
/// <param name="BlockReason">현재 block 사유입니다.</param>
/// <param name="GenealogyLinks">누적 genealogy link 목록입니다.</param>
public sealed record MaterialLotRestoreState(
    MaterialLotId Id,
    string MaterialCode,
    MaterialLotStatus Status,
    MeasuredQuantity AvailableQuantity,
    MeasuredQuantity ConsumedQuantity,
    MeasuredQuantity ReturnedQuantity,
    string? BlockReason,
    IReadOnlyCollection<GenealogyLink> GenealogyLinks);
