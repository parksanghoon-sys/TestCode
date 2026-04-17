using Mes.Application.Contracts.OperatorExecution;

namespace Mes.Client.Wpf.Shell;

/// <summary>
/// 스테이션 작업 큐 항목을 UI 바인딩 형식으로 변환합니다.
/// </summary>
public sealed class WorkQueueItemViewModel
{
    /// <summary>
    /// 계약 큐 항목을 UI용 뷰모델로 변환합니다.
    /// </summary>
    /// <param name="contract">원본 작업 큐 계약입니다.</param>
    public WorkQueueItemViewModel(WorkQueueItemContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        ProductionOrderId = contract.ProductionOrderId;
        OperationExecutionId = contract.OperationExecutionId;
        OperationSequence = contract.OperationSequence;
        StationId = contract.StationId;
        Status = contract.Status;
        OperationQuantityUnit = contract.OperationQuantityUnit;
        QualityGateState = contract.QualityGateState;
        RequiredMaterials = contract.RequiredMaterials;
        RequiredMaterialsSummary = BuildRequiredMaterialsSummary(contract.RequiredMaterials);
        PreferredMaterialCode = contract.RequiredMaterials.FirstOrDefault()?.MaterialCode;
        PreferredMaterialQuantityUnit = contract.RequiredMaterials.FirstOrDefault()?.RequiredQuantity.Unit;
    }

    /// <summary>
    /// 생산 오더 식별자를 가져옵니다.
    /// </summary>
    public string ProductionOrderId { get; }

    /// <summary>
    /// 공정 실행 식별자를 가져옵니다.
    /// </summary>
    public string OperationExecutionId { get; }

    /// <summary>
    /// 공정 순번을 가져옵니다.
    /// </summary>
    public int OperationSequence { get; }

    /// <summary>
    /// 작업 큐 항목에 대응하는 스테이션 식별자를 가져옵니다.
    /// </summary>
    public string StationId { get; }

    /// <summary>
    /// 현재 공정 실행 상태를 가져옵니다.
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// 현재 공정 실행의 authoritative 수량 단위를 가져옵니다.
    /// </summary>
    public string OperationQuantityUnit { get; }

    /// <summary>
    /// 현재 quality gate 상태를 가져옵니다.
    /// </summary>
    public string QualityGateState { get; }

    /// <summary>
    /// 원본 요구 자재 목록을 가져옵니다.
    /// </summary>
    public IReadOnlyList<RequiredMaterialContract> RequiredMaterials { get; }

    /// <summary>
    /// 요구 자재 목록을 화면에 보여 줄 요약 문자열을 가져옵니다.
    /// </summary>
    public string RequiredMaterialsSummary { get; }

    /// <summary>
    /// 요구 자재가 하나 이상 있으면 <see langword="true"/>를 반환합니다.
    /// </summary>
    public bool HasRequiredMaterials => RequiredMaterials.Count > 0;

    /// <summary>
    /// 기본 자재 코드로 사용할 첫 요구 자재 코드를 가져옵니다.
    /// </summary>
    public string? PreferredMaterialCode { get; }

    /// <summary>
    /// 기본 자재 단위로 사용할 첫 요구 자재 단위를 가져옵니다.
    /// </summary>
    public string? PreferredMaterialQuantityUnit { get; }

    /// <summary>
    /// 현재 상태에서 시작 명령을 보낼 수 있는지 여부를 가져옵니다.
    /// </summary>
    public bool CanStartOperation =>
        string.Equals(Status, "Ready", StringComparison.Ordinal)
        || string.Equals(Status, "Queued", StringComparison.Ordinal);

    /// <summary>
    /// 현재 상태에서 자재 투입 명령을 보낼 수 있는지 여부를 가져옵니다.
    /// </summary>
    public bool CanRecordMaterialConsumption =>
        string.Equals(Status, "Running", StringComparison.Ordinal)
        || string.Equals(Status, "Rework", StringComparison.Ordinal);

    /// <summary>
    /// 현재 상태에서 완료 명령을 보낼 수 있는지 여부를 가져옵니다.
    /// </summary>
    public bool CanCompleteOperation => CanRecordMaterialConsumption;

    /// <summary>
    /// 요구 자재 목록을 UI 표시용 문자열로 요약합니다.
    /// </summary>
    /// <param name="requiredMaterials">요약할 요구 자재 목록입니다.</param>
    /// <returns>요약 문자열입니다.</returns>
    private static string BuildRequiredMaterialsSummary(IReadOnlyList<RequiredMaterialContract> requiredMaterials)
    {
        if (requiredMaterials.Count == 0)
        {
            return "없음";
        }

        return string.Join(
            ", ",
            requiredMaterials.Select(
                material => $"{material.MaterialCode} {material.RequiredQuantity.Value:0.###} {material.RequiredQuantity.Unit}"));
    }
}
