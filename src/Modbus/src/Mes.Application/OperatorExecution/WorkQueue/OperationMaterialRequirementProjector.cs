using Mes.Domain.ValueObjects;

namespace Mes.Application.OperatorExecution.WorkQueue;

/// <summary>
/// operation-attachment 기준으로 작업 큐용 자재 요구량 snapshot을 생성합니다.
/// </summary>
public sealed class OperationMaterialRequirementProjector
{
    /// <summary>
    /// 공정 실행에 고정된 자재 요구량 snapshot을 생성합니다.
    /// </summary>
    /// <param name="request">snapshot 생성 요청입니다.</param>
    /// <returns>sequence 기준으로 정렬된 요구량 snapshot 목록입니다.</returns>
    public IReadOnlyList<OperationMaterialRequirementSnapshot> ProjectFromOperationAttachment(
        ProjectOperationMaterialRequirementsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Requirements);

        var snapshots = new List<OperationMaterialRequirementSnapshot>();
        var usedSequences = new HashSet<int>();

        foreach (var requirement in request.Requirements.OrderBy(r => r.SequenceNo))
        {
            ValidateRequirement(requirement, usedSequences);

            snapshots.Add(new OperationMaterialRequirementSnapshot(
                CreateSnapshotId(request.OperationExecutionId, requirement.SequenceNo),
                request.OperationExecutionId,
                requirement.MaterialCode.Trim(),
                requirement.RequiredQuantity,
                new OperationMaterialRequirementMetadata(
                    requirement.SequenceNo,
                    NormalizeOptional(requirement.SourceRevisionRef),
                    request.ProjectedAt)));
        }

        return snapshots;
    }

    /// <summary>
    /// 자재 요구량 입력 항목의 기본 유효성을 검사합니다.
    /// </summary>
    /// <param name="requirement">검사할 자재 요구량 입력입니다.</param>
    /// <param name="usedSequences">이미 사용된 순번 집합입니다.</param>
    private static void ValidateRequirement(
        OperationMaterialRequirementInput requirement,
        ISet<int> usedSequences)
    {
        ArgumentNullException.ThrowIfNull(requirement);

        if (requirement.SequenceNo <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requirement), "Requirement sequence must be greater than zero.");
        }

        if (!usedSequences.Add(requirement.SequenceNo))
        {
            throw new InvalidOperationException("Requirement sequence must be unique within one operation snapshot.");
        }

        if (string.IsNullOrWhiteSpace(requirement.MaterialCode))
        {
            throw new InvalidOperationException("Material code is required for a requirement snapshot.");
        }
    }

    /// <summary>
    /// deterministic snapshot 식별자를 생성합니다.
    /// </summary>
    /// <param name="operationExecutionId">공정 실행 식별자입니다.</param>
    /// <param name="sequenceNo">자재 요구량 순번입니다.</param>
    /// <returns>동일 입력에서 항상 같은 snapshot 식별자입니다.</returns>
    private static string CreateSnapshotId(OperationExecutionId operationExecutionId, int sequenceNo)
    {
        return $"{operationExecutionId}-REQ-{sequenceNo:D3}";
    }

    /// <summary>
    /// 선택 문자열 값을 저장용으로 정규화합니다.
    /// </summary>
    /// <param name="value">정규화할 문자열입니다.</param>
    /// <returns>정규화된 값 또는 <see langword="null"/>입니다.</returns>
    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
