using System.Collections.ObjectModel;
using MilStd1553.Host.Models;
using MilStd1553.Host.Services;

namespace MilStd1553.Host.Tests;

/// <summary>
/// BM JSONL replay reader가 telemetry DTO를 복원하는지 검증합니다.
/// </summary>
public sealed class JsonLinesTelemetryReplayReaderTests
{
    /// <summary>
    /// BM JSONL line들을 읽어 telemetry event 목록으로 복원해야 합니다.
    /// </summary>
    [Fact]
    public void ReadFromFile_WithBusMonitorJsonLines_ReturnsTelemetryRecords()
    {
        var replayPath = Path.Combine(
            Path.GetTempPath(),
            $"milstd1553-replay-{Guid.NewGuid():N}.jsonl");

        try
        {
            File.WriteAllLines(
                replayPath,
                [
                    """{"eventType":"MessageFrame","timeTagMicros":125,"activeBus":"A","description":"메시지 프레임","messageFrame":{"commandWordRaw":6209,"statusWordRaw":2048,"dataWords":[4660,22136],"busLine":"A","timeTagMicros":125}}""",
                    """{"eventType":"AutoFailover","timeTagMicros":250,"activeBus":"B","description":"연속 timeout으로 standby bus로 자동 전환","messageFrame":null}""",
                ]);

            var reader = new JsonLinesTelemetryReplayReader();

            var events = reader.ReadFromFile(replayPath);

            Assert.Equal(2, events.Count);
            Assert.Equal(TelemetryEventType.MessageFrame, events[0].Type);
            Assert.NotNull(events[0].MessageFrame);
            Assert.Equal((ushort)6209, events[0].MessageFrame!.CommandWordRaw);
            Assert.Equal(TelemetryEventType.AutoFailover, events[1].Type);
            Assert.Equal(BusLine.B, events[1].ActiveBus);
        }
        finally
        {
            if (File.Exists(replayPath))
            {
                File.Delete(replayPath);
            }
        }
    }
}
