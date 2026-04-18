using System.Text;
using MilStd1553.Host.Contracts;
using MilStd1553.Host.Models;

namespace MilStd1553.Host.Services;

/// <summary>
/// 세션 실행 결과를 Markdown 보고서로 직렬화합니다.
/// </summary>
public sealed class MarkdownSessionReportExporter : IReportExporter
{
    /// <summary>
    /// 실행 결과를 Markdown 문자열로 내보냅니다.
    /// </summary>
    /// <param name="report">직렬화할 실행 결과입니다.</param>
    /// <returns>요약과 telemetry 표를 포함한 Markdown 문자열입니다.</returns>
    public string Export(SessionExecutionReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# 세션 실행 보고서");
        builder.AppendLine();
        builder.AppendLine("## 요약");
        builder.AppendLine();
        builder.AppendLine($"- Session: `{report.SessionHandle.Value}`");
        builder.AppendLine($"- Scenario: `{report.Scenario.Name}`");
        builder.AppendLine($"- Active Bus: `{report.HealthSnapshot.ActiveBus}`");
        builder.AppendLine($"- Standby Bus: `{report.HealthSnapshot.StandbyBus}`");
        builder.AppendLine($"- Timeout Count: `{report.HealthSnapshot.TimeoutCount}`");
        builder.AppendLine($"- Retry Count: `{report.HealthSnapshot.RetryCount}`");
        builder.AppendLine($"- Degraded: `{report.HealthSnapshot.Degraded}`");
        builder.AppendLine();
        builder.AppendLine("## Telemetry Events");
        builder.AppendLine();
        builder.AppendLine("| Type | TimeTagMicros | ActiveBus | Description |");
        builder.AppendLine("| --- | ---: | --- | --- |");

        foreach (var eventRecord in report.TelemetryEvents)
        {
            builder.AppendLine(
                $"| {eventRecord.Type} | {eventRecord.TimeTagMicros} | {eventRecord.ActiveBus} | {EscapePipe(eventRecord.Description)} |");
        }

        return builder.ToString().TrimEnd();
    }

    private static string EscapePipe(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }
}
