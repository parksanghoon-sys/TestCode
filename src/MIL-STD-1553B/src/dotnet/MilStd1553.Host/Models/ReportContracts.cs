using System.Collections.ObjectModel;

namespace MilStd1553.Host.Models;

/// <summary>
/// Host가 내보내는 세션 실행 결과를 표현합니다.
/// </summary>
/// <param name="SessionHandle">대상 세션 핸들입니다.</param>
/// <param name="Scenario">실행한 시나리오 정의입니다.</param>
/// <param name="HealthSnapshot">보고서 생성 시점의 health snapshot입니다.</param>
/// <param name="TelemetryEvents">보고서에 포함할 telemetry 이벤트 목록입니다.</param>
/// <param name="ExportedAt">보고서 생성 시각입니다.</param>
public sealed record SessionExecutionReport(
    SessionHandle SessionHandle,
    ScenarioDefinition Scenario,
    BusHealthSnapshot HealthSnapshot,
    ReadOnlyCollection<TelemetryEventRecord> TelemetryEvents,
    DateTimeOffset ExportedAt);
