using Mes.Domain.Abstractions;
using Mes.Domain.Common;
using Mes.Domain.Events;
using Mes.Domain.Statuses;
using Mes.Domain.ValueObjects;

namespace Mes.Domain.Aggregates;

/// <summary>
/// 공정 중 품질 판정과 Hold 게이트를 관리하는 애그리거트입니다.
/// </summary>
public sealed class QualityRecord : AggregateRoot<QualityRecordId>
{
    /// <summary>
    /// 품질 기록 애그리거트를 초기화합니다.
    /// </summary>
    /// <param name="id">품질 기록 식별자입니다.</param>
    /// <param name="wipUnitId">대상 WIP 단위 식별자입니다.</param>
    /// <param name="inspectionCode">검사 항목 코드입니다.</param>
    private QualityRecord(
        QualityRecordId id,
        WipUnitId wipUnitId,
        string inspectionCode) : base(id)
    {
        WipUnitId = wipUnitId;
        InspectionCode = DomainGuard.NotWhiteSpace(inspectionCode, nameof(inspectionCode));
        Status = QualityRecordStatus.Pending;
    }

    public WipUnitId WipUnitId { get; }

    public string InspectionCode { get; }

    public QualityRecordStatus Status { get; private set; }

    public string? HoldReason { get; private set; }

    public string? DecisionNote { get; private set; }

    /// <summary>
    /// 새로운 품질 기록을 생성합니다.
    /// </summary>
    /// <param name="id">품질 기록 식별자입니다.</param>
    /// <param name="wipUnitId">대상 WIP 단위 식별자입니다.</param>
    /// <param name="inspectionCode">검사 항목 코드입니다.</param>
    /// <returns>초기 상태의 품질 기록입니다.</returns>
    public static QualityRecord Create(
        QualityRecordId id,
        WipUnitId wipUnitId,
        string inspectionCode)
    {
        return new QualityRecord(id, wipUnitId, inspectionCode);
    }

    /// <summary>
    /// 품질 기록을 검사 진행 상태로 전환합니다.
    /// </summary>
    public void BeginInspection()
    {
        DomainGuard.Against(Status != QualityRecordStatus.Pending, "Inspection can only begin from pending state.");
        Status = QualityRecordStatus.InInspection;
    }

    /// <summary>
    /// 품질 검사 결과를 합격으로 기록합니다.
    /// </summary>
    /// <param name="note">판정 메모입니다.</param>
    /// <param name="occurredAt">기록 시각입니다.</param>
    public void RecordPass(string note, DateTimeOffset occurredAt)
    {
        EnsureInspectable();
        DecisionNote = DomainGuard.NotWhiteSpace(note, nameof(note));
        Status = QualityRecordStatus.Passed;
        Raise(new QualityResultRecordedDomainEvent(Id, Status, occurredAt));
    }

    /// <summary>
    /// 품질 검사 결과를 불합격으로 기록합니다.
    /// </summary>
    /// <param name="note">판정 메모입니다.</param>
    /// <param name="occurredAt">기록 시각입니다.</param>
    public void RecordFail(string note, DateTimeOffset occurredAt)
    {
        EnsureInspectable();
        DecisionNote = DomainGuard.NotWhiteSpace(note, nameof(note));
        Status = QualityRecordStatus.Failed;
        Raise(new QualityResultRecordedDomainEvent(Id, Status, occurredAt));
    }

    /// <summary>
    /// 품질 기록에 Hold를 설정합니다.
    /// </summary>
    /// <param name="reason">Hold 사유입니다.</param>
    /// <param name="occurredAt">Hold 적용 시각입니다.</param>
    public void PlaceHold(string reason, DateTimeOffset occurredAt)
    {
        DomainGuard.Against(Status == QualityRecordStatus.Released, "Released quality records cannot be held again.");
        HoldReason = DomainGuard.NotWhiteSpace(reason, nameof(reason));
        Status = QualityRecordStatus.Hold;
        Raise(new HoldPlacedDomainEvent(nameof(QualityRecord), Id.ToString(), HoldReason, occurredAt));
    }

    /// <summary>
    /// Hold 상태의 품질 기록을 해제합니다.
    /// </summary>
    /// <param name="note">해제 메모입니다.</param>
    /// <param name="occurredAt">해제 시각입니다.</param>
    public void ReleaseHold(string note, DateTimeOffset occurredAt)
    {
        DomainGuard.Against(Status != QualityRecordStatus.Hold, "Only a held quality record can be released.");
        HoldReason = null;
        DecisionNote = DomainGuard.NotWhiteSpace(note, nameof(note));
        Status = QualityRecordStatus.Released;
        Raise(new HoldReleasedDomainEvent(nameof(QualityRecord), Id.ToString(), DecisionNote, occurredAt));
    }

    /// <summary>
    /// 품질 결과를 기록할 수 있는 상태인지 확인합니다.
    /// </summary>
    private void EnsureInspectable()
    {
        DomainGuard.Against(Status is not (QualityRecordStatus.Pending or QualityRecordStatus.InInspection), "Quality result can only be recorded from pending or in-inspection state.");
    }
}
