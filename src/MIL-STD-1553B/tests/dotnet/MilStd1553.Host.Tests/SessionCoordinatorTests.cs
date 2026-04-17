using System.Collections.ObjectModel;
using MilStd1553.Host.Contracts;
using MilStd1553.Host.Exceptions;
using MilStd1553.Host.Models;
using MilStd1553.Host.Services;

namespace MilStd1553.Host.Tests;

/// <summary>
/// 세션 코디네이터의 Host 오케스트레이션 동작을 검증합니다.
/// </summary>
public sealed class SessionCoordinatorTests
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
    /// 잘못된 JSON은 네이티브 호출 전에 거부되어야 합니다.
    /// </summary>
    [Fact]
    public async Task StartFromJsonAsync_InvalidJson_ThrowsValidationException()
    {
        const string invalidScenarioJson = """
            {
              "channelId": -1,
              "activeBus": "A",
              "bcSchedules": [
                {
                  "name": "",
                  "periodMs": 0,
                  "rtAddress": 99,
                  "subAddress": 0,
                  "direction": "Invalid"
                }
              ]
            }
            """;

        var nativeClient = new FakeNativeHarnessClient();
        var coordinator = new SessionCoordinator(new ScenarioDefinitionJsonLoader(), nativeClient);

        await Assert.ThrowsAsync<ScenarioValidationException>(() => coordinator.StartFromJsonAsync(invalidScenarioJson, CancellationToken.None));
        Assert.Equal(0, nativeClient.OpenSessionCallCount);
        Assert.False(coordinator.HasActiveSession);
    }

    /// <summary>
    /// 정상 시작 후 중복 시작은 차단되어야 합니다.
    /// </summary>
    [Fact]
    public async Task StartFromJsonAsync_DuplicateStart_ThrowsInvalidOperationException()
    {
        var nativeClient = new FakeNativeHarnessClient();
        var coordinator = new SessionCoordinator(new ScenarioDefinitionJsonLoader(), nativeClient);

        var result = await coordinator.StartFromJsonAsync(ValidScenarioJson, CancellationToken.None);

        Assert.True(coordinator.HasActiveSession);
        Assert.Equal("session-001", result.SessionHandle.Value);
        Assert.Equal(1, nativeClient.OpenSessionCallCount);

        await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.StartFromJsonAsync(ValidScenarioJson, CancellationToken.None));
    }

    /// <summary>
    /// 활성 세션이 없으면 버스 전환이 차단되어야 합니다.
    /// </summary>
    [Fact]
    public async Task SwitchBusAsync_WithoutSession_ThrowsInvalidOperationException()
    {
        var nativeClient = new FakeNativeHarnessClient();
        var coordinator = new SessionCoordinator(new ScenarioDefinitionJsonLoader(), nativeClient);

        await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.SwitchBusAsync(BusLine.B, CancellationToken.None));
        Assert.Equal(0, nativeClient.SwitchBusCallCount);
    }

    /// <summary>
    /// 세션 중지 후 활성 세션 상태가 정리되어야 합니다.
    /// </summary>
    [Fact]
    public async Task StopAsync_AfterStart_ClearsActiveSession()
    {
        var nativeClient = new FakeNativeHarnessClient();
        var coordinator = new SessionCoordinator(new ScenarioDefinitionJsonLoader(), nativeClient);

        await coordinator.StartFromJsonAsync(ValidScenarioJson, CancellationToken.None);
        await coordinator.StopAsync(CancellationToken.None);

        Assert.False(coordinator.HasActiveSession);
        Assert.Equal(1, nativeClient.StopSessionCallCount);
    }

    private sealed class FakeNativeHarnessClient : INativeHarnessClient
    {
        public int OpenSessionCallCount { get; private set; }

        public int StopSessionCallCount { get; private set; }

        public int SwitchBusCallCount { get; private set; }

        public Task<SessionStartResult> OpenSessionAsync(
            ScenarioDefinition scenario,
            CancellationToken cancellationToken)
        {
            OpenSessionCallCount++;
            return Task.FromResult(new SessionStartResult(
                new SessionHandle("session-001"),
                new BusHealthSnapshot(BusLine.A, BusLine.B, 0, 0, false)));
        }

        public Task StopSessionAsync(
            SessionHandle sessionHandle,
            CancellationToken cancellationToken)
        {
            StopSessionCallCount++;
            return Task.CompletedTask;
        }

        public Task SwitchBusAsync(
            SessionHandle sessionHandle,
            BusLine busLine,
            CancellationToken cancellationToken)
        {
            SwitchBusCallCount++;
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
            return Task.FromResult(new ReadOnlyCollection<TelemetryEventRecord>([]));
        }
    }
}
