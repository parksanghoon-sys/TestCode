using System.Collections.ObjectModel;
using MilStd1553.Host.Contracts;
using MilStd1553.Host.Models;
using MilStd1553.Host.Services;

namespace MilStd1553.Host.Tests;

/// <summary>
/// telemetry 조회 서비스의 세션 경계와 DTO 흐름을 검증합니다.
/// </summary>
public sealed class TelemetryQueryServiceTests
{
    private const string ValidScenarioJson = """
        {
          "name": "기본시나리오",
          "channelId": 0,
          "activeBus": "A",
          "bcSchedules": [
            {
              "name": "PollRt1Status",
              "periodMs": 20,
              "rtAddress": 1,
              "subAddress": 2,
              "direction": "Receive"
            }
          ]
        }
        """;

    /// <summary>
    /// 활성 세션이 없으면 telemetry 조회가 차단되어야 합니다.
    /// </summary>
    [Fact]
    public async Task PollTelemetryAsync_WithoutSession_ThrowsInvalidOperationException()
    {
        var nativeClient = new FakeNativeHarnessClient();
        var coordinator = new SessionCoordinator(new ScenarioDefinitionJsonLoader(), nativeClient);
        var queryService = new TelemetryQueryService(coordinator, nativeClient);

        await Assert.ThrowsAsync<InvalidOperationException>(() => queryService.PollTelemetryAsync(CancellationToken.None));
        Assert.Equal(0, nativeClient.PollTelemetryCallCount);
    }

    /// <summary>
    /// 활성 세션이 있으면 네이티브 telemetry DTO를 그대로 반환해야 합니다.
    /// </summary>
    [Fact]
    public async Task PollTelemetryAsync_AfterStart_ReturnsNativeEvents()
    {
        var nativeClient = new FakeNativeHarnessClient();
        var coordinator = new SessionCoordinator(new ScenarioDefinitionJsonLoader(), nativeClient);
        var queryService = new TelemetryQueryService(coordinator, nativeClient);

        await coordinator.StartFromJsonAsync(ValidScenarioJson, CancellationToken.None);

        var events = await queryService.PollTelemetryAsync(CancellationToken.None);

        Assert.Single(events);
        Assert.Equal(1, nativeClient.PollTelemetryCallCount);
        Assert.Equal(TelemetryEventType.MessageFrame, events[0].Type);
        Assert.NotNull(events[0].MessageFrame);
        Assert.Equal((ushort)0x1841, events[0].MessageFrame!.CommandWordRaw);
        Assert.Equal(BusLine.A, events[0].ActiveBus);
    }

    private sealed class FakeNativeHarnessClient : INativeHarnessClient
    {
        public int PollTelemetryCallCount { get; private set; }

        public Task<SessionStartResult> OpenSessionAsync(
            ScenarioDefinition scenario,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new SessionStartResult(
                new SessionHandle("session-telemetry"),
                new BusHealthSnapshot(BusLine.A, BusLine.B, 0, 0, false)));
        }

        public Task StopSessionAsync(
            SessionHandle sessionHandle,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task SwitchBusAsync(
            SessionHandle sessionHandle,
            BusLine busLine,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<BusHealthSnapshot> GetHealthSnapshotAsync(
            SessionHandle sessionHandle,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new BusHealthSnapshot(BusLine.A, BusLine.B, 0, 0, false));
        }

        public Task<ReadOnlyCollection<TelemetryEventRecord>> PollTelemetryAsync(
            SessionHandle sessionHandle,
            CancellationToken cancellationToken)
        {
            PollTelemetryCallCount++;

            return Task.FromResult(new ReadOnlyCollection<TelemetryEventRecord>(
            [
                new TelemetryEventRecord(
                    TelemetryEventType.MessageFrame,
                    125,
                    BusLine.A,
                    "메시지 프레임",
                    new TelemetryMessageFrame(
                        0x1841,
                        0x0800,
                        new ReadOnlyCollection<ushort>([0x1234]),
                        BusLine.A,
                        125))
            ]));
        }
    }
}
