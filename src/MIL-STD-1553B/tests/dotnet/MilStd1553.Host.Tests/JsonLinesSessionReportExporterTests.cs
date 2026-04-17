using System.Collections.ObjectModel;
using System.Text.Json;
using MilStd1553.Host.Models;
using MilStd1553.Host.Services;

namespace MilStd1553.Host.Tests;

/// <summary>
/// JSONL 보고서 exporter의 요약 및 이벤트 직렬화를 검증합니다.
/// </summary>
public sealed class JsonLinesSessionReportExporterTests
{
    /// <summary>
    /// telemetry가 있으면 summary line과 event line이 함께 생성되어야 합니다.
    /// </summary>
    [Fact]
    public void Export_WithTelemetryEvents_ReturnsSummaryAndEventLines()
    {
        var exporter = new JsonLinesSessionReportExporter();
        var report = new SessionExecutionReport(
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
                    TelemetryEventType.BusSwitch,
                    200,
                    BusLine.B,
                    "활성 버스를 B로 전환",
                    null),
            ]),
            new DateTimeOffset(2026, 4, 17, 9, 30, 0, TimeSpan.Zero));

        var exported = exporter.Export(report);
        var lines = exported.Split(Environment.NewLine, StringSplitOptions.None);

        Assert.Equal(3, lines.Length);

        using var summaryDocument = JsonDocument.Parse(lines[0]);
        Assert.Equal("summary", summaryDocument.RootElement.GetProperty("recordType").GetString());
        Assert.Equal("기본상태조회", summaryDocument.RootElement.GetProperty("scenarioName").GetString());
        Assert.Equal("session-001", summaryDocument.RootElement.GetProperty("sessionId").GetString());
        Assert.Equal(2, summaryDocument.RootElement.GetProperty("totalEvents").GetInt32());
        Assert.Equal(1, summaryDocument.RootElement.GetProperty("messageFrameEvents").GetInt32());
        Assert.Equal(1, summaryDocument.RootElement.GetProperty("busSwitchEvents").GetInt32());
        Assert.True(summaryDocument.RootElement.GetProperty("degraded").GetBoolean());

        using var messageDocument = JsonDocument.Parse(lines[1]);
        Assert.Equal("event", messageDocument.RootElement.GetProperty("recordType").GetString());
        Assert.Equal("MessageFrame", messageDocument.RootElement.GetProperty("eventType").GetString());
        Assert.Equal(0x1841, messageDocument.RootElement.GetProperty("messageFrame").GetProperty("commandWordRaw").GetUInt16());

        using var switchDocument = JsonDocument.Parse(lines[2]);
        Assert.Equal("BusSwitch", switchDocument.RootElement.GetProperty("eventType").GetString());
        Assert.Equal("B", switchDocument.RootElement.GetProperty("activeBus").GetString());
    }

    /// <summary>
    /// telemetry가 없으면 summary line만 생성되어야 합니다.
    /// </summary>
    [Fact]
    public void Export_WithoutTelemetryEvents_ReturnsSummaryOnly()
    {
        var exporter = new JsonLinesSessionReportExporter();
        var report = new SessionExecutionReport(
            new SessionHandle("session-empty"),
            new ScenarioDefinition(
                "빈시나리오",
                0,
                BusLine.A,
                new ReadOnlyCollection<ScenarioScheduleDefinition>([])),
            new BusHealthSnapshot(BusLine.A, BusLine.B, 0, 0, false),
            new ReadOnlyCollection<TelemetryEventRecord>([]),
            new DateTimeOffset(2026, 4, 17, 10, 0, 0, TimeSpan.Zero));

        var exported = exporter.Export(report);
        var lines = exported.Split(Environment.NewLine, StringSplitOptions.None);

        Assert.Single(lines);

        using var summaryDocument = JsonDocument.Parse(lines[0]);
        Assert.Equal(0, summaryDocument.RootElement.GetProperty("totalEvents").GetInt32());
        Assert.Equal(0, summaryDocument.RootElement.GetProperty("messageFrameEvents").GetInt32());
        Assert.Equal(0, summaryDocument.RootElement.GetProperty("busSwitchEvents").GetInt32());
    }
}
