using Mes.Domain.ValueObjects;

namespace Mes.Application.OperatorExecution.WorkQueue;

/// <summary>
/// 작업 큐용 자재 요구량 snapshot 생성에 사용하는 입력 항목입니다.
/// </summary>
/// <param name="SequenceNo">공정 내 자재 순번입니다.</param>
/// <param name="MaterialCode">자재 코드입니다.</param>
/// <param name="RequiredQuantity">요구 수량입니다.</param>
/// <param name="SourceRevisionRef">자재 요구량의 근거 revision reference입니다.</param>
public sealed record OperationMaterialRequirementInput(
    int SequenceNo,
    string MaterialCode,
    MeasuredQuantity RequiredQuantity,
    string? SourceRevisionRef);

/// <summary>
/// operation-attachment 기준의 자재 요구량 snapshot 생성 요청입니다.
/// </summary>
/// <param name="OperationExecutionId">snapshot을 생성할 공정 실행 식별자입니다.</param>
/// <param name="ProjectedAt">snapshot 생성 시각입니다.</param>
/// <param name="Requirements">snapshot으로 고정할 자재 요구량 목록입니다.</param>
public sealed record ProjectOperationMaterialRequirementsRequest(
    OperationExecutionId OperationExecutionId,
    DateTimeOffset ProjectedAt,
    IReadOnlyCollection<OperationMaterialRequirementInput> Requirements);

/// <summary>
/// 자재 요구량 snapshot의 메타데이터입니다.
/// </summary>
/// <param name="SequenceNo">공정 내 자재 순번입니다.</param>
/// <param name="SourceRevisionRef">자재 요구량의 근거 revision reference입니다.</param>
/// <param name="CreatedAt">snapshot 생성 시각입니다.</param>
public sealed record OperationMaterialRequirementMetadata(
    int SequenceNo,
    string? SourceRevisionRef,
    DateTimeOffset CreatedAt);

/// <summary>
/// `GetStationWorkQueue`가 참조하는 MES-side 자재 요구량 snapshot입니다.
/// </summary>
/// <param name="OperationMaterialRequirementId">snapshot 식별자입니다.</param>
/// <param name="OperationExecutionId">연결된 공정 실행 식별자입니다.</param>
/// <param name="MaterialCode">자재 코드입니다.</param>
/// <param name="RequiredQuantity">요구 수량입니다.</param>
/// <param name="Metadata">sequence와 생성 정보입니다.</param>
public sealed record OperationMaterialRequirementSnapshot(
    string OperationMaterialRequirementId,
    OperationExecutionId OperationExecutionId,
    string MaterialCode,
    MeasuredQuantity RequiredQuantity,
    OperationMaterialRequirementMetadata Metadata);
