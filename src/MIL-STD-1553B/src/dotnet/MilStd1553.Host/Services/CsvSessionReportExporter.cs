using System.Globalization;
using System.Text;
using MilStd1553.Host.Contracts;
using MilStd1553.Host.Models;

namespace MilStd1553.Host.Services;

/// <summary>
/// 세션 실행 결과를 CSV 보고서로 직렬화합니다.
/// </summary>
public sealed class CsvSessionReportExporter : IReportExporter
{
    /// <summary>
    /// 실행 결과를 CSV 문자열로 내보냅니다.
    /// </summary>
    /// <param name="report">직렬화할 실행 결과입니다.</param>
    /// <returns>summary 행과 event 행을 포함한 CSV 문자열입니다.</returns>
    public string Export(SessionExecutionReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("recordType,sessionId,scenarioName,channelId,activeBus,standbyBus,timeoutCount,retryCount,degraded,eventType,timeTagMicros,description,commandWordRaw,statusWordRaw,dataWords,busLine,messageTimeTagMicros");
        builder.AppendLine(string.Join(
            ',',
            Escape("summary"),
            Escape(report.SessionHandle.Value),
            Escape(report.Scenario.Name),
            report.Scenario.ChannelId.ToString(CultureInfo.InvariantCulture),
            Escape(report.HealthSnapshot.ActiveBus.ToString()),
            Escape(report.HealthSnapshot.StandbyBus.ToString()),
            report.HealthSnapshot.TimeoutCount.ToString(CultureInfo.InvariantCulture),
            report.HealthSnapshot.RetryCount.ToString(CultureInfo.InvariantCulture),
            report.HealthSnapshot.Degraded ? "true" : "false",
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty));

        foreach (var eventRecord in report.TelemetryEvents)
        {
            builder.AppendLine(string.Join(
                ',',
                Escape("event"),
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                Escape(eventRecord.Type.ToString()),
                eventRecord.TimeTagMicros.ToString(CultureInfo.InvariantCulture),
                Escape(eventRecord.Description),
                eventRecord.MessageFrame?.CommandWordRaw.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                eventRecord.MessageFrame?.StatusWordRaw?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                Escape(eventRecord.MessageFrame is null
                    ? string.Empty
                    : string.Join('|', eventRecord.MessageFrame.DataWords)),
                Escape(eventRecord.MessageFrame?.BusLine.ToString() ?? string.Empty),
                eventRecord.MessageFrame?.TimeTagMicros.ToString(CultureInfo.InvariantCulture) ?? string.Empty));
        }

        return builder.ToString().TrimEnd();
    }

    private static string Escape(string value)
    {
        if (!value.Contains(',', StringComparison.Ordinal)
            && !value.Contains('"', StringComparison.Ordinal)
            && !value.Contains(Environment.NewLine, StringComparison.Ordinal))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
