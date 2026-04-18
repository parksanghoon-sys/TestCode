using System.Collections.ObjectModel;
using MilStd1553.Host.Models;
using MilStd1553.Host.Services;

namespace MilStd1553.Host.Tests;

/// <summary>
/// CSV 보고서 exporter의 최소 summary/event 직렬화를 검증합니다.
/// </summary>
public sealed class CsvSessionReportExporterTests
{
    /// <summary>
    /// CSV exporter는 summary 1행과 event 행들을 같은 출력에 포함해야 합니다.
    /// </summary>
    [Fact]
    public void Export_WithTelemetryEvents_ReturnsSummaryAndEventRows()
    {
        var exporter = new CsvSessionReportExporter();
        var report = CreateReport();

        var exported = exporter.Export(report);
        var lines = exported.Split(Environment.NewLine, StringSplitOptions.None);

        Assert.Equal(4, lines.Length);
        Assert.Contains("recordType,sessionId,scenarioName", lines[0], StringComparison.Ordinal);
        Assert.Contains("summary,session-001,기본상태조회", lines[1], StringComparison.Ordinal);
        Assert.Contains("event,,,,,,", lines[2], StringComparison.Ordinal);
        Assert.Contains("MessageFrame", lines[2], StringComparison.Ordinal);
        Assert.Contains("AutoFailover", lines[3], StringComparison.Ordinal);
    }

    private static SessionExecutionReport CreateReport()
    {
        return new SessionExecutionReport(
            new SessionHandle("session-001"),
            new ScenarioDefinition(
                "기본상태조회",
                2,
                BusLine.A,
                new ReadOnlyCollection<ScenarioScheduleDefinition>([])),
            new BusHealthSnapshot(BusLine.B, BusLine.A, 1, 1, true),
            new ReadOnlyCollection<TelemetryEventRecord>(
            [
                new TelemetryEventRecord(
                    TelemetryEventType.MessageFrame,
                    125,
                    BusLine.A,
                    "메시지 프레임",
                    new TelemetryMessageFrame(
                        0x1841,
                        0x0800,
                        new ReadOnlyCollection<ushort>([0x1234, 0x5678]),
                        BusLine.A,
                        125)),
                new TelemetryEventRecord(
                    TelemetryEventType.AutoFailover,
                    200,
                    BusLine.B,
                    "연속 timeout으로 standby bus로 자동 전환",
                    null),
            ]),
            new DateTimeOffset(2026, 4, 17, 9, 30, 0, TimeSpan.Zero));
    }
}
