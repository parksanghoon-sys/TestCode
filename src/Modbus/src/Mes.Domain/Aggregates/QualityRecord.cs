using Mes.Domain.Abstractions;
using Mes.Domain.Common;
using Mes.Domain.Events;
using Mes.Domain.Statuses;
using Mes.Domain.ValueObjects;

namespace Mes.Domain.Aggregates;

/// <summary>
/// 공정 품질 판정과 hold 게이트 상태를 관리하는 aggregate입니다.
/// </summary>
public sealed class QualityRecord : AggregateRoot<QualityRecordId>
{
    /// <summary>
    /// 품질 기록 aggregate를 초기화합니다.
    /// </summary>
    /// <param name="id">품질 기록 식별자입니다.</param>
    /// <param name="wipUnitId">대상 WIP 식별자입니다.</param>
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

    /// <summary>
    /// 대상 WIP 식별자입니다.
    /// </summary>
    public WipUnitId WipUnitId { get; }

    /// <summary>
    /// 검사 항목 코드입니다.
    /// </summary>
    public string InspectionCode { get; }

    /// <summary>
    /// 현재 품질 기록 상태입니다.
    /// </summary>
    public QualityRecordStatus Status { get; private set; }

    /// <summary>
    /// 마지막으로 기록된 판정 결과입니다.
    /// </summary>
    public QualityDecisionStatus? DecisionStatus { get; private set; }

    /// <summary>
    /// 현재 hold 사유입니다.
    /// </summary>
    public string? HoldReason { get; private set; }

    /// <summary>
    /// 마지막 판정 메모입니다.
    /// </summary>
    public string? DecisionNote { get; private set; }

    /// <summary>
    /// 새로운 품질 기록 aggregate를 생성합니다.
    /// </summary>
    /// <param name="id">품질 기록 식별자입니다.</param>
    /// <param name="wipUnitId">대상 WIP 식별자입니다.</param>
    /// <param name="inspectionCode">검사 항목 코드입니다.</param>
    /// <returns>초기 상태의 품질 기록 aggregate입니다.</returns>
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
        DecisionStatus = QualityDecisionStatus.Passed;
        Status = QualityRecordStatus.Passed;
        Raise(new QualityResultRecordedDomainEvent(Id, DecisionStatus.Value, occurredAt));
    }

    /// <summary>
    /// 품질 검사 결과를 부적합으로 기록합니다.
    /// </summary>
    /// <param name="note">판정 메모입니다.</param>
    /// <param name="occurredAt">기록 시각입니다.</param>
    public void RecordFail(string note, DateTimeOffset occurredAt)
    {
        EnsureInspectable();
        DecisionNote = DomainGuard.NotWhiteSpace(note, nameof(note));
        DecisionStatus = QualityDecisionStatus.Failed;
        Status = QualityRecordStatus.Failed;
        Raise(new QualityResultRecordedDomainEvent(Id, DecisionStatus.Value, occurredAt));
    }

    /// <summary>
    /// 품질 기록을 hold 상태로 전환합니다.
    /// </summary>
    /// <param name="reason">hold 사유입니다.</param>
    /// <param name="occurredAt">hold 시각입니다.</param>
    public void PlaceHold(string reason, DateTimeOffset occurredAt)
    {
        DomainGuard.Against(Status == QualityRecordStatus.Released, "Released quality records cannot be held again.");

        HoldReason = DomainGuard.NotWhiteSpace(reason, nameof(reason));
        Status = QualityRecordStatus.Hold;
        Raise(new HoldPlacedDomainEvent(nameof(QualityRecord), Id.ToString(), HoldReason, occurredAt));
    }

    /// <summary>
    /// hold 상태의 품질 기록을 해제합니다.
    /// </summary>
    /// <param name="note">해제 메모입니다.</param>
    /// <param name="occurredAt">해제 시각입니다.</param>
    public void ReleaseHold(string note, DateTimeOffset occurredAt)
    {
        DomainGuard.Against(Status != QualityRecordStatus.Hold, "Only a held quality record can be released.");

        HoldReason = null;
        Status = QualityRecordStatus.Released;
        Raise(new HoldReleasedDomainEvent(nameof(QualityRecord), Id.ToString(), DomainGuard.NotWhiteSpace(note, nameof(note)), occurredAt));
    }

    /// <summary>
    /// 품질 결과를 기록할 수 있는 상태인지 확인합니다.
    /// </summary>
    private void EnsureInspectable()
    {
        DomainGuard.Against(Status is not (QualityRecordStatus.Pending or QualityRecordStatus.InInspection), "Quality result can only be recorded from pending or in-inspection state.");
    }
}
