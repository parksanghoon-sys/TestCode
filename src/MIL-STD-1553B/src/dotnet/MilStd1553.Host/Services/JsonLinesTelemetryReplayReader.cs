using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using MilStd1553.Host.Contracts;
using MilStd1553.Host.Models;

namespace MilStd1553.Host.Services;

/// <summary>
/// BM JSONL 로그를 telemetry DTO로 복원합니다.
/// </summary>
public sealed class JsonLinesTelemetryReplayReader : ITelemetryReplayReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// 지정한 JSONL 파일에서 telemetry event를 읽습니다.
    /// </summary>
    /// <param name="filePath">읽을 JSONL 파일 경로입니다.</param>
    /// <returns>복원된 telemetry event 목록입니다.</returns>
    public ReadOnlyCollection<TelemetryEventRecord> ReadFromFile(string filePath)
    {
        var events = File.ReadLines(filePath)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line =>
            {
                var payload = JsonSerializer.Deserialize<ReplayTelemetryPayload>(line, JsonOptions)
                    ?? throw new InvalidOperationException("replay JSONL line이 비어 있습니다.");

                return new TelemetryEventRecord(
                    payload.EventType,
                    payload.TimeTagMicros,
                    payload.ActiveBus,
                    payload.Description,
                    payload.MessageFrame is null
                        ? null
                        : new TelemetryMessageFrame(
                            payload.MessageFrame.CommandWordRaw,
                            payload.MessageFrame.StatusWordRaw,
                            new ReadOnlyCollection<ushort>(payload.MessageFrame.DataWords),
                            payload.MessageFrame.BusLine,
                            payload.MessageFrame.TimeTagMicros));
            })
            .ToList();

        return new ReadOnlyCollection<TelemetryEventRecord>(events);
    }

    private sealed record ReplayTelemetryPayload(
        TelemetryEventType EventType,
        long TimeTagMicros,
        BusLine ActiveBus,
        string Description,
        ReplayMessageFramePayload? MessageFrame);

    private sealed record ReplayMessageFramePayload(
        ushort CommandWordRaw,
        ushort? StatusWordRaw,
        List<ushort> DataWords,
        BusLine BusLine,
        long TimeTagMicros);
}
