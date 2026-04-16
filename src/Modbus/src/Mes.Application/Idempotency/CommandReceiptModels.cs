namespace Mes.Application.Idempotency;

/// <summary>
/// command receipt의 자연 유일 범위를 표현합니다.
/// </summary>
/// <param name="Channel">명령이 들어온 채널입니다.</param>
/// <param name="CommandType">canonical command type입니다.</param>
/// <param name="IdempotencyKey">재시도 중복 제거 키입니다.</param>
public sealed record CommandReceiptScope(
    string Channel,
    string CommandType,
    string IdempotencyKey);

/// <summary>
/// 저장된 command receipt의 결과 코드 집합입니다.
/// </summary>
public static class CommandReceiptResultCodeValues
{
    /// <summary>
    /// 명령이 정상 수락된 경우입니다.
    /// </summary>
    public const string Accepted = "accepted";

    /// <summary>
    /// validation 단계에서 결정적으로 거절된 경우입니다.
    /// </summary>
    public const string ValidationRejected = "validation-rejected";
}

/// <summary>
/// 저장 가능한 command receipt 레코드입니다.
/// </summary>
/// <param name="CommandId">서버가 인식한 명령 식별자입니다.</param>
/// <param name="Scope">idempotency 자연 유일 범위입니다.</param>
/// <param name="ActorId">명령을 보낸 주체 식별자입니다.</param>
/// <param name="StationId">스테이션 바운드 명령의 스테이션 식별자입니다.</param>
/// <param name="CorrelationId">같은 작업 대화의 상관 관계 식별자입니다.</param>
/// <param name="RequestFingerprint">canonical business field 기반 fingerprint입니다.</param>
/// <param name="AggregateType">대상 aggregate 유형입니다.</param>
/// <param name="AggregateId">대상 aggregate 식별자입니다.</param>
/// <param name="AcceptedAt">서버 수락 시각입니다.</param>
/// <param name="ResultCode">저장된 결과 코드입니다.</param>
/// <param name="ResponseJson">재생 가능한 정규화 응답 payload입니다.</param>
public sealed record CommandReceiptRecord(
    string CommandId,
    CommandReceiptScope Scope,
    string ActorId,
    string? StationId,
    string CorrelationId,
    string RequestFingerprint,
    string AggregateType,
    string AggregateId,
    DateTimeOffset AcceptedAt,
    string ResultCode,
    string? ResponseJson);

/// <summary>
/// 수락된 command receipt 생성 입력입니다.
/// </summary>
/// <param name="CommandId">저장할 명령 식별자입니다.</param>
/// <param name="Scope">idempotency 자연 유일 범위입니다.</param>
/// <param name="ActorId">명령 주체 식별자입니다.</param>
/// <param name="StationId">스테이션 식별자입니다.</param>
/// <param name="CorrelationId">상관 관계 식별자입니다.</param>
/// <param name="RequestFingerprint">저장할 fingerprint입니다.</param>
/// <param name="AggregateType">대상 aggregate 유형입니다.</param>
/// <param name="AggregateId">대상 aggregate 식별자입니다.</param>
/// <param name="AcceptedAt">수락 시각입니다.</param>
/// <param name="ResponseJson">재생용 정규화 응답 payload입니다.</param>
public sealed record RegisterAcceptedCommandReceiptRequest(
    string CommandId,
    CommandReceiptScope Scope,
    string ActorId,
    string? StationId,
    string CorrelationId,
    string RequestFingerprint,
    string AggregateType,
    string AggregateId,
    DateTimeOffset AcceptedAt,
    string? ResponseJson);

/// <summary>
/// idempotency 판정 종류를 표현합니다.
/// </summary>
public enum CommandReceiptDecisionKind
{
    AcceptNew,
    ReplayStored,
    Conflict
}

/// <summary>
/// 저장된 receipt와 신규 요청을 비교하기 위한 입력입니다.
/// </summary>
/// <param name="Scope">현재 요청의 receipt scope입니다.</param>
/// <param name="RequestFingerprint">현재 요청의 fingerprint입니다.</param>
/// <param name="ExistingReceipt">이미 저장된 receipt입니다.</param>
public sealed record CommandReceiptEvaluationRequest(
    CommandReceiptScope Scope,
    string RequestFingerprint,
    CommandReceiptRecord? ExistingReceipt);

/// <summary>
/// idempotency 판정 결과입니다.
/// </summary>
/// <param name="Decision">판정 종류입니다.</param>
/// <param name="StoredReceipt">재생 또는 충돌 판단에 사용된 기존 receipt입니다.</param>
/// <param name="IncomingFingerprint">현재 요청의 fingerprint입니다.</param>
public sealed record CommandReceiptEvaluationResult(
    CommandReceiptDecisionKind Decision,
    CommandReceiptRecord? StoredReceipt,
    string IncomingFingerprint);
