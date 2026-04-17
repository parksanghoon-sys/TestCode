using System.Text.Json;
using MilStd1553.Host.Contracts;
using MilStd1553.Host.Models;

namespace MilStd1553.Host.Services;

/// <summary>
/// 세션 실행 결과를 JSONL 요약 보고서로 직렬화합니다.
/// </summary>
public sealed class JsonLinesSessionReportExporter : IReportExporter
{
    /// <summary>
    /// 실행 결과를 JSONL 문자열로 내보냅니다.
    /// </summary>
    /// <param name="report">직렬화할 실행 결과입니다.</param>
    /// <returns>summary line과 event line으로 구성된 JSONL 문자열입니다.</returns>
    public string Export(SessionExecutionReport report)
    {
        var lines = new List<string>
        {
            JsonSerializer.Serialize(CreateSummaryLine(report)),
        };

        lines.AddRange(report.TelemetryEvents.Select(eventRecord =>
            JsonSerializer.Serialize(CreateEventLine(eventRecord))));

        return string.Join(Environment.NewLine, lines);
    }

    private static object CreateSummaryLine(SessionExecutionReport report)
    {
        var messageFrameEvents = report.TelemetryEvents.Count(eventRecord =>
            eventRecord.Type == TelemetryEventType.MessageFrame);
        var busSwitchEvents = report.TelemetryEvents.Count(eventRecord =>
            eventRecord.Type == TelemetryEventType.BusSwitch);

        return new
        {
            recordType = "summary",
            sessionId = report.SessionHandle.Value,
            scenarioName = report.Scenario.Name,
            channelId = report.Scenario.ChannelId,
            exportedAtUtc = report.ExportedAt.UtcDateTime.ToString("O"),
            activeBus = report.HealthSnapshot.ActiveBus.ToString(),
            standbyBus = report.HealthSnapshot.StandbyBus.ToString(),
            timeoutCount = report.HealthSnapshot.TimeoutCount,
            retryCount = report.HealthSnapshot.RetryCount,
            degraded = report.HealthSnapshot.Degraded,
            totalEvents = report.TelemetryEvents.Count,
            messageFrameEvents,
            busSwitchEvents,
        };
    }

    private static object CreateEventLine(TelemetryEventRecord eventRecord)
    {
        return new
        {
            recordType = "event",
            eventType = eventRecord.Type.ToString(),
            timeTagMicros = eventRecord.TimeTagMicros,
            activeBus = eventRecord.ActiveBus.ToString(),
            description = eventRecord.Description,
            messageFrame = eventRecord.MessageFrame is null
                ? null
                : new
                {
                    commandWordRaw = eventRecord.MessageFrame.CommandWordRaw,
                    statusWordRaw = eventRecord.MessageFrame.StatusWordRaw,
                    dataWords = eventRecord.MessageFrame.DataWords,
                    busLine = eventRecord.MessageFrame.BusLine.ToString(),
                    timeTagMicros = eventRecord.MessageFrame.TimeTagMicros,
                },
        };
    }
}
