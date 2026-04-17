namespace Mes.Application.Contracts.OperatorExecution;

/// <summary>
/// 운영자 실행 pilot slice 에서 사용하는 canonical command type 을 정의합니다.
/// </summary>
public static class OperatorExecutionCommandTypes
{
    /// <summary>
    /// 공정 실행 시작 명령입니다.
    /// </summary>
    public const string StartOperation = "start-operation";

    /// <summary>
    /// 자재 소모 확정 명령입니다.
    /// </summary>
    public const string RecordMaterialConsumption = "record-material-consumption";

    /// <summary>
    /// 자재 스캔 검증 명령입니다.
    /// </summary>
    public const string RecordMaterialScan = "record-material-scan";

    /// <summary>
    /// hold 설정 명령입니다.
    /// </summary>
    public const string PlaceHold = "place-hold";

    /// <summary>
    /// hold 해제 명령입니다.
    /// </summary>
    public const string ReleaseHold = "release-hold";

    /// <summary>
    /// 품질 판정 기록 명령입니다.
    /// </summary>
    public const string RecordQualityResult = "record-quality-result";

    /// <summary>
    /// 공정 완료 명령입니다.
    /// </summary>
    public const string CompleteOperation = "complete-operation";
}
