namespace Mes.Application.Contracts.Common;

/// <summary>
/// BFF 명령 envelope 에서 사용하는 채널 값을 정의합니다.
/// </summary>
public static class BffChannelValues
{
    /// <summary>
    /// WPF 스테이션 클라이언트 채널을 나타냅니다.
    /// </summary>
    public const string Wpf = "wpf";

    /// <summary>
    /// 웹 포털 채널을 나타냅니다.
    /// </summary>
    public const string Web = "web";

    /// <summary>
    /// 외부 시스템 연계 채널을 나타냅니다.
    /// </summary>
    public const string Integration = "integration";

    /// <summary>
    /// 현장 edge 게이트웨이 채널을 나타냅니다.
    /// </summary>
    public const string Edge = "edge";
}

/// <summary>
/// 요청 시점의 기준 revision 정보를 함께 전달합니다.
/// </summary>
/// <param name="ItemRevision">품목 revision 식별자입니다.</param>
/// <param name="RouteRevision">라우팅 revision 식별자입니다.</param>
/// <param name="BomRevision">BOM revision 식별자입니다.</param>
/// <param name="SpecRevision">검사 또는 작업 지시 revision 식별자입니다.</param>
public sealed record RevisionReferencesContract(
    string? ItemRevision,
    string? RouteRevision,
    string? BomRevision,
    string? SpecRevision);

/// <summary>
/// 수량과 단위를 transport 계층에서 전달하기 위한 값을 나타냅니다.
/// </summary>
/// <param name="Value">수량 값입니다.</param>
/// <param name="Unit">수량 단위입니다.</param>
public sealed record MeasuredQuantityContract(
    decimal Value,
    string Unit);

/// <summary>
/// BFF 명령의 고유 식별과 재시도 추적 metadata를 나타냅니다.
/// </summary>
/// <param name="CommandId">전역 고유 명령 식별자입니다.</param>
/// <param name="CorrelationId">같은 작업 대화를 연결하는 상관관계 식별자입니다.</param>
/// <param name="IdempotencyKey">재시도 중복을 제거하기 위한 키입니다.</param>
public sealed record CommandIdentityContract(
    string CommandId,
    string CorrelationId,
    string IdempotencyKey);

/// <summary>
/// BFF 명령이 어떤 주체와 채널에서 발생했는지 나타냅니다.
/// </summary>
/// <param name="ActorId">명령을 보낸 사용자 또는 시스템 주체입니다.</param>
/// <param name="Channel">명령이 들어온 채널입니다.</param>
/// <param name="StationId">스테이션 고정 명령에서만 사용하는 스테이션 식별자입니다.</param>
public sealed record CommandOriginContract(
    string ActorId,
    string Channel,
    string? StationId);

/// <summary>
/// BFF 명령이 공유하는 공통 metadata 묶음을 나타냅니다.
/// </summary>
/// <param name="Identity">명령 식별과 재시도 추적 정보입니다.</param>
/// <param name="Origin">명령 주체와 채널 정보입니다.</param>
/// <param name="ClientTimestamp">클라이언트가 기록한 전송 시각입니다.</param>
/// <param name="RevisionRefs">명령 해석에 필요한 revision 스냅샷입니다.</param>
public sealed record CommandContextContract(
    CommandIdentityContract Identity,
    CommandOriginContract Origin,
    DateTimeOffset ClientTimestamp,
    RevisionReferencesContract? RevisionRefs);

/// <summary>
/// 모든 BFF 명령이 공유하는 공통 envelope 구조를 정의합니다.
/// </summary>
/// <typeparam name="TPayload">명령 본문 payload 형식입니다.</typeparam>
/// <param name="Context">명령 공통 metadata 묶음입니다.</param>
/// <param name="CommandType">명령 종류입니다.</param>
/// <param name="Payload">명령별 세부 본문입니다.</param>
public abstract record BffCommandEnvelope<TPayload>(
    CommandContextContract Context,
    string CommandType,
    TPayload Payload)
{
    /// <summary>
    /// 전역 고유 명령 식별자를 반환합니다.
    /// </summary>
    public string CommandId => Context.Identity.CommandId;

    /// <summary>
    /// 같은 작업 대화를 연결하는 상관관계 식별자를 반환합니다.
    /// </summary>
    public string CorrelationId => Context.Identity.CorrelationId;

    /// <summary>
    /// 재시도 중복을 제거하기 위한 키를 반환합니다.
    /// </summary>
    public string IdempotencyKey => Context.Identity.IdempotencyKey;

    /// <summary>
    /// 명령을 보낸 사용자 또는 시스템 주체를 반환합니다.
    /// </summary>
    public string ActorId => Context.Origin.ActorId;

    /// <summary>
    /// 명령이 들어온 채널을 반환합니다.
    /// </summary>
    public string Channel => Context.Origin.Channel;

    /// <summary>
    /// 스테이션 고정 명령에서 사용하는 스테이션 식별자를 반환합니다.
    /// </summary>
    public string? StationId => Context.Origin.StationId;

    /// <summary>
    /// 클라이언트가 기록한 전송 시각을 반환합니다.
    /// </summary>
    public DateTimeOffset ClientTimestamp => Context.ClientTimestamp;

    /// <summary>
    /// 명령 해석에 필요한 revision 스냅샷을 반환합니다.
    /// </summary>
    public RevisionReferencesContract? RevisionRefs => Context.RevisionRefs;
}

/// <summary>
/// 모든 BFF 명령 응답이 공유하는 최소 응답 구조를 정의합니다.
/// </summary>
/// <param name="Accepted">명령이 수락되었는지 여부입니다.</param>
/// <param name="CommandId">서버가 인식한 명령 식별자입니다.</param>
/// <param name="ServerReceivedAt">서버가 명령을 수신한 시각입니다.</param>
public abstract record BffCommandResponse(
    bool Accepted,
    string CommandId,
    DateTimeOffset ServerReceivedAt);

/// <summary>
/// BFF endpoint 계약을 문서와 코드에서 함께 추적하기 위한 서명을 정의합니다.
/// </summary>
/// <param name="OperationName">업무 작업 이름입니다.</param>
/// <param name="HttpMethod">HTTP 메서드입니다.</param>
/// <param name="Route">endpoint 경로입니다.</param>
/// <param name="CommandType">명령 endpoint 일 때의 canonical command type 입니다.</param>
/// <param name="RequestType">요청 계약 형식입니다.</param>
/// <param name="ResponseType">응답 계약 형식입니다.</param>
public sealed record BffEndpointSignature(
    string OperationName,
    string HttpMethod,
    string Route,
    string? CommandType,
    Type RequestType,
    Type ResponseType);
