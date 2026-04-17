using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using MilStd1553.Host.Models;
using MilStd1553.Interop.Exceptions;
using MilStd1553.Interop.NativeMethods;
using MilStd1553.Interop.Services;

namespace MilStd1553.Host.Tests;

/// <summary>
/// `NativeHarnessClient`의 JSON 직렬화, 재시도, native payload 매핑을 검증합니다.
/// </summary>
public sealed class NativeHarnessClientTests
{
    /// <summary>
    /// `OpenSessionAsync`는 시나리오를 native JSON으로 직렬화하고 결과를 Host DTO로 변환해야 합니다.
    /// </summary>
    [Fact]
    public async Task OpenSessionAsync_MapsSessionHandleAndHealthSnapshot()
    {
        var nativeApi = new FakeNativeSessionApi();
        nativeApi.OpenSessionResponses.Enqueue(new OpenSessionResponse(
            0,
            "session-native-001",
            """{"activeBus":"B","standbyBus":"A","timeoutCount":1,"retryCount":2,"degraded":true}"""));

        var client = new NativeHarnessClient(nativeApi);
        var scenario = new ScenarioDefinition(
            "기본상태조회",
            3,
            BusLine.B,
            new ReadOnlyCollection<ScenarioScheduleDefinition>(
            [
                new ScenarioScheduleDefinition("PollRt1", 20, 1, 2, TransferDirection.Receive),
            ]));

        var result = await client.OpenSessionAsync(scenario, CancellationToken.None);

        Assert.Equal("session-native-001", result.SessionHandle.Value);
        Assert.Equal(BusLine.B, result.InitialHealth.ActiveBus);
        Assert.Equal(1, result.InitialHealth.TimeoutCount);
        Assert.Equal(2, result.InitialHealth.RetryCount);
        Assert.Contains("\"activeBus\":\"B\"", nativeApi.LastScenarioJson, StringComparison.Ordinal);
        Assert.Contains("\"bcSchedules\"", nativeApi.LastScenarioJson, StringComparison.Ordinal);
        Assert.Contains("\"direction\":\"Receive\"", nativeApi.LastScenarioJson, StringComparison.Ordinal);
    }

    /// <summary>
    /// health snapshot과 telemetry payload는 Host DTO로 매핑되어야 합니다.
    /// </summary>
    [Fact]
    public async Task GetHealthSnapshotAndPollTelemetryAsync_MapNativeJsonPayload()
    {
        var nativeApi = new FakeNativeSessionApi();
        nativeApi.HealthSnapshotResponses.Enqueue(new JsonPayloadResponse(
            0,
            """{"activeBus":"A","standbyBus":"B","timeoutCount":0,"retryCount":1,"degraded":false}"""));
        nativeApi.TelemetryResponses.Enqueue(new JsonPayloadResponse(
            0,
            """
            [
              {
                "type":"MessageFrame",
                "timeTagMicros":125,
                "activeBus":"A",
                "description":"메시지 프레임",
                "messageFrame":{
                  "commandWordRaw":6209,
                  "statusWordRaw":2048,
                  "dataWords":[4660],
                  "busLine":"A",
                  "timeTagMicros":125
                }
              }
            ]
            """));

        var client = new NativeHarnessClient(nativeApi);
        var sessionHandle = new SessionHandle("session-telemetry");

        var health = await client.GetHealthSnapshotAsync(sessionHandle, CancellationToken.None);
        var events = await client.PollTelemetryAsync(sessionHandle, CancellationToken.None);

        Assert.Equal(BusLine.A, health.ActiveBus);
        Assert.Equal(1, health.RetryCount);
        Assert.Single(events);
        Assert.Equal(TelemetryEventType.MessageFrame, events[0].Type);
        Assert.Equal((ushort)6209, events[0].MessageFrame!.CommandWordRaw);
        Assert.Equal("session-telemetry", nativeApi.LastSessionHandle);
    }

    /// <summary>
    /// native status code가 실패면 예외로 변환되어야 합니다.
    /// </summary>
    [Fact]
    public async Task StopSessionAsync_WhenNativeReturnsError_ThrowsNativeInteropException()
    {
        var nativeApi = new FakeNativeSessionApi();
        nativeApi.StopSessionStatuses.Enqueue(2);

        var client = new NativeHarnessClient(nativeApi);

        var exception = await Assert.ThrowsAsync<NativeInteropException>(() =>
            client.StopSessionAsync(new SessionHandle("missing-session"), CancellationToken.None));

        Assert.Equal(2, exception.StatusCode);
    }

    /// <summary>
    /// `OpenSessionAsync`는 `BufferTooSmall` 응답을 받으면 두 출력 버퍼를 함께 재할당해 재시도해야 합니다.
    /// </summary>
    [Fact]
    public async Task OpenSessionAsync_WhenNativeReportsBufferTooSmall_RetriesWithRequiredCapacities()
    {
        var nativeApi = new FakeNativeSessionApi();
        nativeApi.OpenSessionResponses.Enqueue(new OpenSessionResponse(
            3,
            string.Empty,
            string.Empty,
            256,
            8192));
        nativeApi.OpenSessionResponses.Enqueue(new OpenSessionResponse(
            0,
            "session-native-002",
            """{"activeBus":"A","standbyBus":"B","timeoutCount":0,"retryCount":0,"degraded":false}"""));

        var client = new NativeHarnessClient(nativeApi);
        var scenario = new ScenarioDefinition(
            "BufferRetry",
            0,
            BusLine.A,
            new ReadOnlyCollection<ScenarioScheduleDefinition>([]));

        var result = await client.OpenSessionAsync(scenario, CancellationToken.None);

        Assert.Equal("session-native-002", result.SessionHandle.Value);
        Assert.Equal(BusLine.A, result.InitialHealth.ActiveBus);
        Assert.Collection(
            nativeApi.OpenSessionCapacities,
            item =>
            {
                Assert.Equal(128, item.SessionHandleCapacity);
                Assert.Equal(4096, item.HealthJsonCapacity);
            },
            item =>
            {
                Assert.Equal(256, item.SessionHandleCapacity);
                Assert.Equal(8192, item.HealthJsonCapacity);
            });
    }

    /// <summary>
    /// `GetHealthSnapshotAsync`는 `BufferTooSmall` 응답 시 필요 길이로 재시도해야 합니다.
    /// </summary>
    [Fact]
    public async Task GetHealthSnapshotAsync_WhenNativeReportsBufferTooSmall_RetriesWithRequiredCapacity()
    {
        var nativeApi = new FakeNativeSessionApi();
        nativeApi.HealthSnapshotResponses.Enqueue(new JsonPayloadResponse(3, string.Empty, 9000));
        nativeApi.HealthSnapshotResponses.Enqueue(new JsonPayloadResponse(
            0,
            """{"activeBus":"B","standbyBus":"A","timeoutCount":2,"retryCount":3,"degraded":true}"""));

        var client = new NativeHarnessClient(nativeApi);

        var result = await client.GetHealthSnapshotAsync(new SessionHandle("session-health"), CancellationToken.None);

        Assert.Equal(BusLine.B, result.ActiveBus);
        Assert.Equal(2, result.TimeoutCount);
        Assert.Equal(3, result.RetryCount);
        Assert.Collection(
            nativeApi.HealthSnapshotCapacities,
            capacity => Assert.Equal(4096, capacity),
            capacity => Assert.Equal(9000, capacity));
    }

    /// <summary>
    /// `PollTelemetryAsync`는 `BufferTooSmall` 응답 시 필요 길이로 재시도해야 합니다.
    /// </summary>
    [Fact]
    public async Task PollTelemetryAsync_WhenNativeReportsBufferTooSmall_RetriesWithRequiredCapacity()
    {
        var nativeApi = new FakeNativeSessionApi();
        nativeApi.TelemetryResponses.Enqueue(new JsonPayloadResponse(3, string.Empty, 70000));
        nativeApi.TelemetryResponses.Enqueue(new JsonPayloadResponse(
            0,
            """
            [
              {
                "type":"BusSwitch",
                "timeTagMicros":500,
                "activeBus":"B",
                "description":"Bus switched",
                "messageFrame":null
              }
            ]
            """));

        var client = new NativeHarnessClient(nativeApi);

        var result = await client.PollTelemetryAsync(new SessionHandle("session-telemetry"), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(TelemetryEventType.BusSwitch, result[0].Type);
        Assert.Equal(BusLine.B, result[0].ActiveBus);
        Assert.Collection(
            nativeApi.TelemetryCapacities,
            capacity => Assert.Equal(65536, capacity),
            capacity => Assert.Equal(70000, capacity));
    }

    private sealed class FakeNativeSessionApi : INativeSessionApi
    {
        public Queue<OpenSessionResponse> OpenSessionResponses { get; } = new();

        public Queue<int> StopSessionStatuses { get; } = new();

        public Queue<int> SwitchBusStatuses { get; } = new();

        public Queue<JsonPayloadResponse> HealthSnapshotResponses { get; } = new();

        public Queue<JsonPayloadResponse> TelemetryResponses { get; } = new();

        public List<(int SessionHandleCapacity, int HealthJsonCapacity)> OpenSessionCapacities { get; } = [];

        public List<int> HealthSnapshotCapacities { get; } = [];

        public List<int> TelemetryCapacities { get; } = [];

        public string LastScenarioJson { get; private set; } = string.Empty;

        public string LastSessionHandle { get; private set; } = string.Empty;

        public int OpenSession(
            string scenarioJson,
            StringBuilder sessionHandleBuffer,
            int sessionHandleCapacity,
            out int requiredSessionHandleCapacity,
            StringBuilder healthJsonBuffer,
            int healthJsonCapacity,
            out int requiredHealthJsonCapacity)
        {
            LastScenarioJson = scenarioJson;
            OpenSessionCapacities.Add((sessionHandleCapacity, healthJsonCapacity));

            var response = OpenSessionResponses.Count > 0
                ? OpenSessionResponses.Dequeue()
                : new OpenSessionResponse(
                    0,
                    "session-default",
                    """{"activeBus":"A","standbyBus":"B","timeoutCount":0,"retryCount":0,"degraded":false}""");

            requiredSessionHandleCapacity = response.RequiredSessionHandleCapacity;
            requiredHealthJsonCapacity = response.RequiredHealthJsonCapacity;

            if (response.StatusCode == 0)
            {
                sessionHandleBuffer.Append(response.SessionHandlePayload);
                healthJsonBuffer.Append(response.HealthPayload);
            }

            return response.StatusCode;
        }

        public int StopSession(string sessionHandle)
        {
            LastSessionHandle = sessionHandle;
            return StopSessionStatuses.Count > 0 ? StopSessionStatuses.Dequeue() : 0;
        }

        public int SwitchBus(string sessionHandle, int busLine)
        {
            LastSessionHandle = sessionHandle;
            return SwitchBusStatuses.Count > 0 ? SwitchBusStatuses.Dequeue() : 0;
        }

        public int GetHealthSnapshot(
            string sessionHandle,
            StringBuilder healthJsonBuffer,
            int healthJsonCapacity,
            out int requiredHealthJsonCapacity)
        {
            LastSessionHandle = sessionHandle;
            HealthSnapshotCapacities.Add(healthJsonCapacity);

            var response = HealthSnapshotResponses.Count > 0
                ? HealthSnapshotResponses.Dequeue()
                : new JsonPayloadResponse(
                    0,
                    """{"activeBus":"A","standbyBus":"B","timeoutCount":0,"retryCount":0,"degraded":false}""");

            requiredHealthJsonCapacity = response.RequiredCapacity;
            if (response.StatusCode == 0)
            {
                healthJsonBuffer.Append(response.Payload);
            }

            return response.StatusCode;
        }

        public int PollTelemetry(
            string sessionHandle,
            StringBuilder telemetryJsonBuffer,
            int telemetryJsonCapacity,
            out int requiredTelemetryJsonCapacity)
        {
            LastSessionHandle = sessionHandle;
            TelemetryCapacities.Add(telemetryJsonCapacity);

            var response = TelemetryResponses.Count > 0
                ? TelemetryResponses.Dequeue()
                : new JsonPayloadResponse(0, "[]");

            requiredTelemetryJsonCapacity = response.RequiredCapacity;
            if (response.StatusCode == 0)
            {
                telemetryJsonBuffer.Append(response.Payload);
            }

            return response.StatusCode;
        }
    }

    private sealed record OpenSessionResponse(
        int StatusCode,
        string SessionHandlePayload,
        string HealthPayload,
        int RequiredSessionHandleCapacity = 0,
        int RequiredHealthJsonCapacity = 0);

    private sealed record JsonPayloadResponse(
        int StatusCode,
        string Payload,
        int RequiredCapacity = 0);
}
