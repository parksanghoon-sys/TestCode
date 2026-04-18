using System.Collections.ObjectModel;
using MilStd1553.Host.Models;
using MilStd1553.Host.Services;

namespace MilStd1553.Host.Tests;

/// <summary>
/// Markdown 보고서 exporter의 요약과 telemetry 표 출력을 검증합니다.
/// </summary>
public sealed class MarkdownSessionReportExporterTests
{
    /// <summary>
    /// Markdown exporter는 요약 섹션과 telemetry 표를 함께 출력해야 합니다.
    /// </summary>
    [Fact]
    public void Export_WithTelemetryEvents_ReturnsMarkdownSections()
    {
        var exporter = new MarkdownSessionReportExporter();
        var report = new SessionExecutionReport(
            new SessionHandle("session-002"),
            new ScenarioDefinition(
                "벡터워드조회",
                7,
                BusLine.B,
                new ReadOnlyCollection<ScenarioScheduleDefinition>([])),
            new BusHealthSnapshot(BusLine.B, BusLine.A, 2, 1, true),
            new ReadOnlyCollection<TelemetryEventRecord>(
            [
                new TelemetryEventRecord(
                    TelemetryEventType.AutoFailover,
                    640,
                    BusLine.B,
                    "line fault로 standby bus로 자동 전환",
                    null),
            ]),
            new DateTimeOffset(2026, 4, 17, 10, 0, 0, TimeSpan.Zero));

        var exported = exporter.Export(report);

        Assert.Contains("# 세션 실행 보고서", exported, StringComparison.Ordinal);
        Assert.Contains("## 요약", exported, StringComparison.Ordinal);
        Assert.Contains("## Telemetry Events", exported, StringComparison.Ordinal);
        Assert.Contains("| AutoFailover | 640 | B | line fault로 standby bus로 자동 전환 |", exported, StringComparison.Ordinal);
    }
}
