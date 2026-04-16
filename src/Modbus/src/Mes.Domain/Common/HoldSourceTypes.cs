namespace Mes.Domain.Common;

/// <summary>
/// Hold 출처를 식별할 때 사용하는 canonical 값 모음입니다.
/// </summary>
public static class HoldSourceTypes
{
    /// <summary>
    /// 품질 기록이 생성한 hold 출처입니다.
    /// </summary>
    public const string QualityRecord = "quality-record";

    /// <summary>
    /// 현장 수동 조치가 생성한 hold 출처입니다.
    /// </summary>
    public const string Manual = "manual";
}
