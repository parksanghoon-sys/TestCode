using Mes.Domain.Abstractions;
using Mes.Domain.Aggregates;
using Mes.Domain.Events;
using Mes.Domain.Statuses;
using Mes.Domain.ValueObjects;

namespace Mes.Domain.Tests;

/// <summary>
/// 품질 기록 애그리거트의 Hold 제어 규칙을 검증합니다.
/// </summary>
public class QualityRecordTests
{
    /// <summary>
    /// Hold 해제는 Hold 상태에서만 허용되는지 검증합니다.
    /// </summary>
    [Fact]
    public void Hold_release_should_only_work_from_hold_state()
    {
        var qualityRecord = QualityRecord.Create(
            new QualityRecordId("QR-1001"),
            new WipUnitId("WIP-1001"),
            "INSP-01");

        Assert.Throws<DomainException>(() => qualityRecord.ReleaseHold("not allowed", DateTimeOffset.UtcNow));

        qualityRecord.BeginInspection();
        qualityRecord.PlaceHold("dimension mismatch", DateTimeOffset.UtcNow);
        qualityRecord.ReleaseHold("rechecked and approved", DateTimeOffset.UtcNow);

        Assert.Equal(QualityRecordStatus.Released, qualityRecord.Status);
        Assert.Contains(qualityRecord.DomainEvents, e => e is HoldPlacedDomainEvent);
        Assert.Contains(qualityRecord.DomainEvents, e => e is HoldReleasedDomainEvent);
    }
}
