using Mes.Application.Contracts.Common;

namespace Mes.Application.Contracts.OperatorExecution;

/// <summary>
/// hold 명령이 참조할 수 있는 주체 종류를 정의합니다.
/// </summary>
public static class HoldSubjectTypeValues
{
    /// <summary>
    /// 공정 실행 주체입니다.
    /// </summary>
    public const string OperationExecution = "operation-execution";

    /// <summary>
    /// WIP 단위 주체입니다.
    /// </summary>
    public const string WipUnit = "wip-unit";

    /// <summary>
    /// 품질 기록 주체입니다.
    /// </summary>
    public const string QualityRecord = "quality-record";
}

/// <summary>
/// 품질 판정 값의 canonical 표현을 정의합니다.
/// </summary>
public static class QualityDecisionValues
{
    /// <summary>
    /// 합격 판정입니다.
    /// </summary>
    public const string Passed = "passed";

    /// <summary>
    /// 불합격 판정입니다.
    /// </summary>
    public const string Failed = "failed";
}

/// <summary>
/// 공정 완료가 입력된 방식을 정의합니다.
/// </summary>
public static class CompletionModeValues
{
    /// <summary>
    /// 작업자가 수동으로 완료를 입력한 경우입니다.
    /// </summary>
    public const string Manual = "manual";

    /// <summary>
    /// 설비 보조 입력으로 완료를 확정한 경우입니다.
    /// </summary>
    public const string EquipmentAssisted = "equipment-assisted";
}

/// <summary>
/// 작업 큐와 상태 조회에서 사용할 품질 gate 상태 값을 정의합니다.
/// </summary>
public static class QualityGateStateValues
{
    /// <summary>
    /// 품질 gate 가 열려 있어 다음 진행이 가능한 상태입니다.
    /// </summary>
    public const string Open = "open";

    /// <summary>
    /// 품질 gate 가 hold 상태인 경우입니다.
    /// </summary>
    public const string Hold = "hold";

    /// <summary>
    /// 추가 검토가 필요해 작업 완료를 막는 상태입니다.
    /// </summary>
    public const string ReviewRequired = "review-required";
}

/// <summary>
/// 생산실적 준비 상태의 초기 표현을 정의합니다.
/// </summary>
public static class ProductionActualsStatusValues
{
    /// <summary>
    /// 도메인 완료 이후 projection 준비를 기다리는 상태입니다.
    /// </summary>
    public const string PendingProjection = "pending-projection";
}

/// <summary>
/// 공정 시작 명령의 본문 payload 입니다.
/// </summary>
/// <param name="ProductionOrderId">대상 생산오더 식별자입니다.</param>
/// <param name="OperationExecutionId">시작할 공정 실행 식별자입니다.</param>
/// <param name="OperationSequence">라우팅 상 공정 순번입니다.</param>
/// <param name="QuantityUnit">기본 수량 단위입니다.</param>
public sealed record StartOperationPayloadContract(
    string ProductionOrderId,
    string OperationExecutionId,
    int OperationSequence,
    string? QuantityUnit);

/// <summary>
/// 공정 시작 명령 계약입니다.
/// </summary>
/// <param name="CommandId">전역 고유 명령 식별자입니다.</param>
/// <param name="ActorId">명령을 요청한 작업자 또는 시스템 식별자입니다.</param>
/// <param name="Channel">명령 입력 채널입니다.</param>
/// <param name="StationId">작업 스테이션 식별자입니다.</param>
/// <param name="CorrelationId">같은 작업 대화를 연결하는 상관관계 식별자입니다.</param>
/// <param name="IdempotencyKey">재시도 중복 제거 키입니다.</param>
/// <param name="ClientTimestamp">클라이언트 전송 시각입니다.</param>
/// <param name="RevisionRefs">작업 해석에 필요한 revision 스냅샷입니다.</param>
/// <param name="Payload">공정 시작 본문입니다.</param>
public sealed record StartOperationCommandContract(
    CommandContextContract Context,
    StartOperationPayloadContract Payload)
    : BffCommandEnvelope<StartOperationPayloadContract>(
        Context,
        OperatorExecutionCommandTypes.StartOperation,
        Payload);

/// <summary>
/// 공정 시작 명령의 수락 응답입니다.
/// </summary>
/// <param name="Accepted">명령 수락 여부입니다.</param>
/// <param name="CommandId">서버가 인식한 명령 식별자입니다.</param>
/// <param name="ServerReceivedAt">서버 수신 시각입니다.</param>
/// <param name="OperationExecutionId">시작된 공정 실행 식별자입니다.</param>
/// <param name="Status">정규화된 현재 상태입니다.</param>
/// <param name="StartedAt">정규화된 시작 시각입니다.</param>
public sealed record StartOperationResponseContract(
    bool Accepted,
    string CommandId,
    DateTimeOffset ServerReceivedAt,
    string OperationExecutionId,
    string Status,
    DateTimeOffset StartedAt)
    : BffCommandResponse(
        Accepted,
        CommandId,
        ServerReceivedAt);

/// <summary>
/// 자재 소모 확정 명령의 본문 payload 입니다.
/// </summary>
/// <param name="OperationExecutionId">활성 공정 실행 식별자입니다.</param>
/// <param name="WipUnitId">자재가 귀속될 WIP 식별자입니다.</param>
/// <param name="MaterialLotId">소모할 자재 lot 식별자입니다.</param>
/// <param name="MaterialCode">작업자 확인용 자재 코드입니다.</param>
/// <param name="Quantity">소모 수량입니다.</param>
public sealed record RecordMaterialConsumptionPayloadContract(
    string OperationExecutionId,
    string WipUnitId,
    string MaterialLotId,
    string MaterialCode,
    MeasuredQuantityContract Quantity);

/// <summary>
/// 자재 소모 확정 명령 계약입니다.
/// </summary>
/// <param name="CommandId">전역 고유 명령 식별자입니다.</param>
/// <param name="ActorId">명령을 요청한 작업자 또는 시스템 식별자입니다.</param>
/// <param name="Channel">명령 입력 채널입니다.</param>
/// <param name="StationId">작업 스테이션 식별자입니다.</param>
/// <param name="CorrelationId">같은 작업 대화를 연결하는 상관관계 식별자입니다.</param>
/// <param name="IdempotencyKey">재시도 중복 제거 키입니다.</param>
/// <param name="ClientTimestamp">클라이언트 전송 시각입니다.</param>
/// <param name="RevisionRefs">작업 해석에 필요한 revision 스냅샷입니다.</param>
/// <param name="Payload">자재 소모 본문입니다.</param>
public sealed record RecordMaterialConsumptionCommandContract(
    CommandContextContract Context,
    RecordMaterialConsumptionPayloadContract Payload)
    : BffCommandEnvelope<RecordMaterialConsumptionPayloadContract>(
        Context,
        OperatorExecutionCommandTypes.RecordMaterialConsumption,
        Payload);

/// <summary>
/// 자재 소모 확정 명령의 수락 응답입니다.
/// </summary>
/// <param name="Accepted">명령 수락 여부입니다.</param>
/// <param name="CommandId">서버가 인식한 명령 식별자입니다.</param>
/// <param name="ServerReceivedAt">서버 수신 시각입니다.</param>
/// <param name="MaterialLotId">적용된 자재 lot 식별자입니다.</param>
/// <param name="RemainingQuantity">소모 후 남은 수량입니다.</param>
/// <param name="GenealogyLinkCreated">genealogy link 생성 여부입니다.</param>
public sealed record RecordMaterialConsumptionResponseContract(
    bool Accepted,
    string CommandId,
    DateTimeOffset ServerReceivedAt,
    string MaterialLotId,
    MeasuredQuantityContract RemainingQuantity,
    bool GenealogyLinkCreated)
    : BffCommandResponse(
        Accepted,
        CommandId,
        ServerReceivedAt);

/// <summary>
/// hold 설정 명령의 본문 payload 입니다.
/// </summary>
/// <param name="SubjectType">hold 대상 주체 종류입니다.</param>
/// <param name="SubjectId">hold 대상 식별자입니다.</param>
/// <param name="Reason">hold 사유입니다.</param>
public sealed record PlaceHoldPayloadContract(
    string SubjectType,
    string SubjectId,
    string Reason);

/// <summary>
/// hold 설정 명령 계약입니다.
/// </summary>
/// <param name="CommandId">전역 고유 명령 식별자입니다.</param>
/// <param name="ActorId">명령을 요청한 작업자 또는 시스템 식별자입니다.</param>
/// <param name="Channel">명령 입력 채널입니다.</param>
/// <param name="StationId">작업 스테이션 식별자입니다.</param>
/// <param name="CorrelationId">같은 작업 대화를 연결하는 상관관계 식별자입니다.</param>
/// <param name="IdempotencyKey">재시도 중복 제거 키입니다.</param>
/// <param name="ClientTimestamp">클라이언트 전송 시각입니다.</param>
/// <param name="RevisionRefs">작업 해석에 필요한 revision 스냅샷입니다.</param>
/// <param name="Payload">hold 설정 본문입니다.</param>
public sealed record PlaceHoldCommandContract(
    CommandContextContract Context,
    PlaceHoldPayloadContract Payload)
    : BffCommandEnvelope<PlaceHoldPayloadContract>(
        Context,
        OperatorExecutionCommandTypes.PlaceHold,
        Payload);

/// <summary>
/// hold 설정 명령의 수락 응답입니다.
/// </summary>
/// <param name="Accepted">명령 수락 여부입니다.</param>
/// <param name="CommandId">서버가 인식한 명령 식별자입니다.</param>
/// <param name="ServerReceivedAt">서버 수신 시각입니다.</param>
/// <param name="SubjectType">hold 가 적용된 주체 종류입니다.</param>
/// <param name="SubjectId">hold 가 적용된 주체 식별자입니다.</param>
/// <param name="Status">정규화된 현재 상태입니다.</param>
public sealed record PlaceHoldResponseContract(
    bool Accepted,
    string CommandId,
    DateTimeOffset ServerReceivedAt,
    string SubjectType,
    string SubjectId,
    string Status)
    : BffCommandResponse(
        Accepted,
        CommandId,
        ServerReceivedAt);

/// <summary>
/// hold 해제 명령의 본문 payload 입니다.
/// </summary>
/// <param name="SubjectType">hold 를 해제할 주체 종류입니다.</param>
/// <param name="SubjectId">hold 를 해제할 주체 식별자입니다.</param>
/// <param name="Note">해제 근거 메모입니다.</param>
public sealed record ReleaseHoldPayloadContract(
    string SubjectType,
    string SubjectId,
    string Note);

/// <summary>
/// hold 해제 명령 계약입니다.
/// </summary>
/// <param name="CommandId">전역 고유 명령 식별자입니다.</param>
/// <param name="ActorId">명령을 요청한 작업자 또는 시스템 식별자입니다.</param>
/// <param name="Channel">명령 입력 채널입니다.</param>
/// <param name="StationId">작업 스테이션 식별자입니다.</param>
/// <param name="CorrelationId">같은 작업 대화를 연결하는 상관관계 식별자입니다.</param>
/// <param name="IdempotencyKey">재시도 중복 제거 키입니다.</param>
/// <param name="ClientTimestamp">클라이언트 전송 시각입니다.</param>
/// <param name="RevisionRefs">작업 해석에 필요한 revision 스냅샷입니다.</param>
/// <param name="Payload">hold 해제 본문입니다.</param>
public sealed record ReleaseHoldCommandContract(
    CommandContextContract Context,
    ReleaseHoldPayloadContract Payload)
    : BffCommandEnvelope<ReleaseHoldPayloadContract>(
        Context,
        OperatorExecutionCommandTypes.ReleaseHold,
        Payload);

/// <summary>
/// hold 해제 명령의 수락 응답입니다.
/// </summary>
/// <param name="Accepted">명령 수락 여부입니다.</param>
/// <param name="CommandId">서버가 인식한 명령 식별자입니다.</param>
/// <param name="ServerReceivedAt">서버 수신 시각입니다.</param>
/// <param name="SubjectType">hold 가 해제된 주체 종류입니다.</param>
/// <param name="SubjectId">hold 가 해제된 주체 식별자입니다.</param>
/// <param name="Status">정규화된 현재 상태입니다.</param>
public sealed record ReleaseHoldResponseContract(
    bool Accepted,
    string CommandId,
    DateTimeOffset ServerReceivedAt,
    string SubjectType,
    string SubjectId,
    string Status)
    : BffCommandResponse(
        Accepted,
        CommandId,
        ServerReceivedAt);

/// <summary>
/// 품질 판정 기록 명령의 본문 payload 입니다.
/// </summary>
/// <param name="QualityRecordId">품질 기록 식별자입니다.</param>
/// <param name="WipUnitId">검사 대상 WIP 식별자입니다.</param>
/// <param name="InspectionCode">검사 항목 또는 공정 checkpoint 코드입니다.</param>
/// <param name="Decision">합격 또는 불합격 판정 값입니다.</param>
/// <param name="Note">검사자 메모입니다.</param>
public sealed record RecordQualityResultPayloadContract(
    string QualityRecordId,
    string WipUnitId,
    string InspectionCode,
    string Decision,
    string Note);

/// <summary>
/// 품질 판정 기록 명령 계약입니다.
/// </summary>
/// <param name="CommandId">전역 고유 명령 식별자입니다.</param>
/// <param name="ActorId">명령을 요청한 작업자 또는 시스템 식별자입니다.</param>
/// <param name="Channel">명령 입력 채널입니다.</param>
/// <param name="StationId">작업 스테이션 식별자입니다.</param>
/// <param name="CorrelationId">같은 작업 대화를 연결하는 상관관계 식별자입니다.</param>
/// <param name="IdempotencyKey">재시도 중복 제거 키입니다.</param>
/// <param name="ClientTimestamp">클라이언트 전송 시각입니다.</param>
/// <param name="RevisionRefs">작업 해석에 필요한 revision 스냅샷입니다.</param>
/// <param name="Payload">품질 판정 본문입니다.</param>
public sealed record RecordQualityResultCommandContract(
    CommandContextContract Context,
    RecordQualityResultPayloadContract Payload)
    : BffCommandEnvelope<RecordQualityResultPayloadContract>(
        Context,
        OperatorExecutionCommandTypes.RecordQualityResult,
        Payload);

/// <summary>
/// 품질 판정 기록 명령의 수락 응답입니다.
/// </summary>
/// <param name="Accepted">명령 수락 여부입니다.</param>
/// <param name="CommandId">서버가 인식한 명령 식별자입니다.</param>
/// <param name="ServerReceivedAt">서버 수신 시각입니다.</param>
/// <param name="QualityRecordId">적용된 품질 기록 식별자입니다.</param>
/// <param name="Status">정규화된 현재 상태입니다.</param>
/// <param name="QualityGateOpen">품질 gate 가 열려 있는지 여부입니다.</param>
public sealed record RecordQualityResultResponseContract(
    bool Accepted,
    string CommandId,
    DateTimeOffset ServerReceivedAt,
    string QualityRecordId,
    string Status,
    bool QualityGateOpen)
    : BffCommandResponse(
        Accepted,
        CommandId,
        ServerReceivedAt);

/// <summary>
/// 공정 완료 명령의 본문 payload 입니다.
/// </summary>
/// <param name="OperationExecutionId">완료할 공정 실행 식별자입니다.</param>
/// <param name="GoodQuantity">양품 수량입니다.</param>
/// <param name="ScrapQuantity">동일 화면에서 함께 입력하는 불량 수량입니다.</param>
/// <param name="CompletionMode">완료 입력 방식입니다.</param>
public sealed record CompleteOperationPayloadContract(
    string OperationExecutionId,
    MeasuredQuantityContract GoodQuantity,
    MeasuredQuantityContract? ScrapQuantity,
    string? CompletionMode);

/// <summary>
/// 공정 완료 명령 계약입니다.
/// </summary>
/// <param name="CommandId">전역 고유 명령 식별자입니다.</param>
/// <param name="ActorId">명령을 요청한 작업자 또는 시스템 식별자입니다.</param>
/// <param name="Channel">명령 입력 채널입니다.</param>
/// <param name="StationId">작업 스테이션 식별자입니다.</param>
/// <param name="CorrelationId">같은 작업 대화를 연결하는 상관관계 식별자입니다.</param>
/// <param name="IdempotencyKey">재시도 중복 제거 키입니다.</param>
/// <param name="ClientTimestamp">클라이언트 전송 시각입니다.</param>
/// <param name="RevisionRefs">작업 해석에 필요한 revision 스냅샷입니다.</param>
/// <param name="Payload">공정 완료 본문입니다.</param>
public sealed record CompleteOperationCommandContract(
    CommandContextContract Context,
    CompleteOperationPayloadContract Payload)
    : BffCommandEnvelope<CompleteOperationPayloadContract>(
        Context,
        OperatorExecutionCommandTypes.CompleteOperation,
        Payload);

/// <summary>
/// 공정 완료 명령의 수락 응답입니다.
/// </summary>
/// <param name="Accepted">명령 수락 여부입니다.</param>
/// <param name="CommandId">서버가 인식한 명령 식별자입니다.</param>
/// <param name="ServerReceivedAt">서버 수신 시각입니다.</param>
/// <param name="OperationExecutionId">완료된 공정 실행 식별자입니다.</param>
/// <param name="Status">정규화된 현재 상태입니다.</param>
/// <param name="CompletedAt">정규화된 완료 시각입니다.</param>
/// <param name="ProductionActualsStatus">후속 생산실적 projection 상태입니다.</param>
public sealed record CompleteOperationResponseContract(
    bool Accepted,
    string CommandId,
    DateTimeOffset ServerReceivedAt,
    string OperationExecutionId,
    string Status,
    DateTimeOffset CompletedAt,
    string ProductionActualsStatus)
    : BffCommandResponse(
        Accepted,
        CommandId,
        ServerReceivedAt);

/// <summary>
/// 스테이션 작업 큐 조회 요청 계약입니다.
/// </summary>
/// <param name="StationId">조회 대상 스테이션 식별자입니다.</param>
public sealed record GetStationWorkQueueRequestContract(
    string StationId);

/// <summary>
/// 작업 큐 항목에 함께 내려주는 요구 자재 요약입니다.
/// </summary>
/// <param name="MaterialCode">요구 자재 코드입니다.</param>
/// <param name="RequiredQuantity">필요 수량입니다.</param>
public sealed record RequiredMaterialContract(
    string MaterialCode,
    MeasuredQuantityContract RequiredQuantity);

/// <summary>
/// 스테이션 작업 큐에 표시할 공정 실행 요약입니다.
/// </summary>
/// <param name="ProductionOrderId">생산오더 식별자입니다.</param>
/// <param name="OperationExecutionId">공정 실행 식별자입니다.</param>
/// <param name="OperationSequence">라우팅 상 공정 순번입니다.</param>
/// <param name="StationId">바인딩된 스테이션 식별자입니다.</param>
/// <param name="Status">현재 공정 실행 상태입니다.</param>
/// <param name="RequiredMaterials">요구 자재 요약 목록입니다.</param>
/// <param name="QualityGateState">현재 품질 gate 상태입니다.</param>
public sealed record WorkQueueItemContract(
    string ProductionOrderId,
    string OperationExecutionId,
    int OperationSequence,
    string StationId,
    string Status,
    IReadOnlyList<RequiredMaterialContract> RequiredMaterials,
    string QualityGateState);

/// <summary>
/// 스테이션 작업 큐 조회 응답 계약입니다.
/// </summary>
/// <param name="StationId">조회한 스테이션 식별자입니다.</param>
/// <param name="SnapshotTakenAt">응답 스냅샷 시각입니다.</param>
/// <param name="Items">현재 작업 큐 항목 목록입니다.</param>
public sealed record GetStationWorkQueueResponseContract(
    string StationId,
    DateTimeOffset SnapshotTakenAt,
    IReadOnlyList<WorkQueueItemContract> Items);

/// <summary>
/// 공정 상태 변경을 채널별 UI 에 전달하는 알림 계약입니다.
/// </summary>
/// <param name="EventType">도메인 또는 workflow 이벤트 이름입니다.</param>
/// <param name="OperationExecutionId">영향받은 공정 실행 식별자입니다.</param>
/// <param name="ProductionOrderId">부모 생산오더 식별자입니다.</param>
/// <param name="Status">정규화된 현재 상태입니다.</param>
/// <param name="OccurredAt">상태가 발생한 시각입니다.</param>
/// <param name="CorrelationId">원인 명령과 연결하는 상관관계 식별자입니다.</param>
public sealed record OperationStateChangedNotificationContract(
    string EventType,
    string OperationExecutionId,
    string ProductionOrderId,
    string Status,
    DateTimeOffset OccurredAt,
    string CorrelationId);
