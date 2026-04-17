using Mes.Application.Contracts.Common;

namespace Mes.Application.Contracts.OperatorExecution;

/// <summary>
/// 운영자 실행 pilot slice 의 BFF endpoint 서명을 한곳에서 정의합니다.
/// </summary>
public static class OperatorExecutionEndpointSignatures
{
    private const string BaseRoute = "/api/bff/operator-execution";

    /// <summary>
    /// 스테이션 작업 큐 조회 endpoint 서명입니다.
    /// </summary>
    public static BffEndpointSignature GetStationWorkQueue { get; } =
        new(
            "GetStationWorkQueue",
            "GET",
            $"{BaseRoute}/stations/{{stationId}}/work-queue",
            null,
            typeof(GetStationWorkQueueRequestContract),
            typeof(GetStationWorkQueueResponseContract));

    /// <summary>
    /// 공정 시작 endpoint 서명입니다.
    /// </summary>
    public static BffEndpointSignature StartOperation { get; } =
        new(
            "StartOperation",
            "POST",
            $"{BaseRoute}/commands/start-operation",
            OperatorExecutionCommandTypes.StartOperation,
            typeof(StartOperationCommandContract),
            typeof(StartOperationResponseContract));

    /// <summary>
    /// 자재 소모 확정 endpoint 서명입니다.
    /// </summary>
    public static BffEndpointSignature RecordMaterialConsumption { get; } =
        new(
            "RecordMaterialConsumption",
            "POST",
            $"{BaseRoute}/commands/material-consumption",
            OperatorExecutionCommandTypes.RecordMaterialConsumption,
            typeof(RecordMaterialConsumptionCommandContract),
            typeof(RecordMaterialConsumptionResponseContract));

    /// <summary>
    /// 자재 스캔 검증 endpoint 서명입니다.
    /// </summary>
    public static BffEndpointSignature RecordMaterialScan { get; } =
        new(
            "RecordMaterialScan",
            "POST",
            $"{BaseRoute}/commands/material-scan",
            OperatorExecutionCommandTypes.RecordMaterialScan,
            typeof(RecordMaterialScanCommandContract),
            typeof(RecordMaterialScanResponseContract));

    /// <summary>
    /// hold 설정 endpoint 서명입니다.
    /// </summary>
    public static BffEndpointSignature PlaceHold { get; } =
        new(
            "PlaceHold",
            "POST",
            $"{BaseRoute}/commands/place-hold",
            OperatorExecutionCommandTypes.PlaceHold,
            typeof(PlaceHoldCommandContract),
            typeof(PlaceHoldResponseContract));

    /// <summary>
    /// hold 해제 endpoint 서명입니다.
    /// </summary>
    public static BffEndpointSignature ReleaseHold { get; } =
        new(
            "ReleaseHold",
            "POST",
            $"{BaseRoute}/commands/release-hold",
            OperatorExecutionCommandTypes.ReleaseHold,
            typeof(ReleaseHoldCommandContract),
            typeof(ReleaseHoldResponseContract));

    /// <summary>
    /// 품질 판정 기록 endpoint 서명입니다.
    /// </summary>
    public static BffEndpointSignature RecordQualityResult { get; } =
        new(
            "RecordQualityResult",
            "POST",
            $"{BaseRoute}/commands/quality-result",
            OperatorExecutionCommandTypes.RecordQualityResult,
            typeof(RecordQualityResultCommandContract),
            typeof(RecordQualityResultResponseContract));

    /// <summary>
    /// 공정 완료 endpoint 서명입니다.
    /// </summary>
    public static BffEndpointSignature CompleteOperation { get; } =
        new(
            "CompleteOperation",
            "POST",
            $"{BaseRoute}/commands/complete-operation",
            OperatorExecutionCommandTypes.CompleteOperation,
            typeof(CompleteOperationCommandContract),
            typeof(CompleteOperationResponseContract));

    /// <summary>
    /// 운영자 실행 slice 가 노출하는 endpoint 서명 전체를 반환합니다.
    /// </summary>
    /// <returns>정의된 endpoint 서명 목록입니다.</returns>
    public static IReadOnlyList<BffEndpointSignature> GetAll()
    {
        return
        [
            GetStationWorkQueue,
            StartOperation,
            RecordMaterialScan,
            RecordMaterialConsumption,
            PlaceHold,
            ReleaseHold,
            RecordQualityResult,
            CompleteOperation
        ];
    }
}
