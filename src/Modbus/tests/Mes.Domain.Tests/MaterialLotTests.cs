using Mes.Domain.Aggregates;
using Mes.Domain.Events;
using Mes.Domain.Statuses;
using Mes.Domain.ValueObjects;

namespace Mes.Domain.Tests;

/// <summary>
/// 자재 Lot 애그리거트의 소모 및 추적성 연결을 검증합니다.
/// </summary>
public class MaterialLotTests
{
    /// <summary>
    /// 자재 소모 시 가용 수량 감소와 genealogy 연결 생성이 함께 반영되는지 검증합니다.
    /// </summary>
    [Fact]
    public void Consume_should_reduce_available_quantity_and_create_genealogy_link()
    {
        var lot = MaterialLot.Create(
            new MaterialLotId("LOT-1001"),
            "MAT-01",
            new MeasuredQuantity(5, "EA"));

        lot.IssueToLine();
        lot.ConsumeFor(new WipUnitId("WIP-1001"), new MeasuredQuantity(2, "EA"), DateTimeOffset.UtcNow);

        Assert.Equal(3, lot.AvailableQuantity.Value);
        Assert.Equal(2, lot.ConsumedQuantity.Value);
        Assert.Equal(MaterialLotStatus.Issued, lot.Status);
        Assert.Single(lot.GenealogyLinks);
        Assert.Contains(lot.DomainEvents, e => e is MaterialConsumptionRecordedDomainEvent);
        Assert.Contains(lot.DomainEvents, e => e is GenealogyLinkCreatedDomainEvent);
    }
}
