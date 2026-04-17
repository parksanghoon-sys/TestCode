using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MilStd1553.Host.Contracts;
using MilStd1553.Host.Models;
using MilStd1553.Interop.Exceptions;
using MilStd1553.Interop.NativeMethods;

namespace MilStd1553.Interop.Services;

/// <summary>
/// 네이티브 세션 중심 C ABI와 Host DTO 사이를 매핑합니다.
/// </summary>
public sealed class NativeHarnessClient : INativeHarnessClient
{
    private delegate int NativeBufferOperation(
        StringBuilder buffer,
        int capacity,
        out int requiredCapacity);

    private const int BufferTooSmallStatusCode = 3;
    private const int MaxBufferResizeAttempts = 3;
    private const int SessionHandleBufferCapacity = 128;
    private const int HealthJsonBufferCapacity = 4096;
    private const int TelemetryJsonBufferCapacity = 65536;

    private static readonly JsonSerializerOptions ScenarioJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly INativeSessionApi nativeSessionApi;

    /// <summary>
    /// 기본 네이티브 런타임 로더를 사용하는 클라이언트를 생성합니다.
    /// </summary>
    public NativeHarnessClient()
        : this(new PInvokeNativeSessionApi())
    {
    }

    /// <summary>
    /// 지정한 네이티브 API 구현으로 클라이언트를 생성합니다.
    /// </summary>
    /// <param name="nativeSessionApi">호출할 네이티브 API 구현입니다.</param>
    internal NativeHarnessClient(INativeSessionApi nativeSessionApi)
    {
        this.nativeSessionApi = nativeSessionApi;
    }

    /// <summary>
    /// 네이티브 세션을 시작합니다.
    /// </summary>
    /// <param name="scenario">시작할 시나리오입니다.</param>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>세션 시작 결과입니다.</returns>
    public Task<SessionStartResult> OpenSessionAsync(
        ScenarioDefinition scenario,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var scenarioJson = SerializeScenario(scenario);
        var sessionHandleCapacity = SessionHandleBufferCapacity;
        var healthJsonCapacity = HealthJsonBufferCapacity;

        for (var attempt = 0; attempt < MaxBufferResizeAttempts; attempt++)
        {
            var sessionHandleBuffer = new StringBuilder(sessionHandleCapacity);
            var healthJsonBuffer = new StringBuilder(healthJsonCapacity);
            var status = nativeSessionApi.OpenSession(
                scenarioJson,
                sessionHandleBuffer,
                sessionHandleCapacity,
                out var requiredSessionHandleCapacity,
                healthJsonBuffer,
                healthJsonCapacity,
                out var requiredHealthJsonCapacity);

            if (status == 0)
            {
                var sessionHandle = new SessionHandle(sessionHandleBuffer.ToString());
                var healthSnapshot = DeserializeRequired<BusHealthSnapshot>(
                    healthJsonBuffer.ToString(),
                    "OpenSession health snapshot");

                return Task.FromResult(new SessionStartResult(sessionHandle, healthSnapshot));
            }

            if (status != BufferTooSmallStatusCode)
            {
                EnsureSuccess(status, "OpenSession");
            }

            sessionHandleCapacity = GetNextBufferCapacity(sessionHandleCapacity, requiredSessionHandleCapacity);
            healthJsonCapacity = GetNextBufferCapacity(healthJsonCapacity, requiredHealthJsonCapacity);
        }

        throw CreateBufferRetryLimitExceededException("OpenSession");
    }

    /// <summary>
    /// 네이티브 세션을 종료합니다.
    /// </summary>
    /// <param name="sessionHandle">종료할 세션 핸들입니다.</param>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>비동기 작업입니다.</returns>
    public Task StopSessionAsync(
        SessionHandle sessionHandle,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var status = nativeSessionApi.StopSession(sessionHandle.Value);
        EnsureSuccess(status, "StopSession");

        return Task.CompletedTask;
    }

    /// <summary>
    /// 네이티브 세션의 활성 버스를 전환합니다.
    /// </summary>
    /// <param name="sessionHandle">대상 세션 핸들입니다.</param>
    /// <param name="busLine">전환할 버스입니다.</param>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>비동기 작업입니다.</returns>
    public Task SwitchBusAsync(
        SessionHandle sessionHandle,
        BusLine busLine,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var status = nativeSessionApi.SwitchBus(sessionHandle.Value, (int)busLine);
        EnsureSuccess(status, "SwitchBus");

        return Task.CompletedTask;
    }

    /// <summary>
    /// 네이티브 세션의 health snapshot을 조회합니다.
    /// </summary>
    /// <param name="sessionHandle">대상 세션 핸들입니다.</param>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>조회된 health snapshot입니다.</returns>
    public Task<BusHealthSnapshot> GetHealthSnapshotAsync(
        SessionHandle sessionHandle,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        int Invoke(StringBuilder buffer, int capacity, out int requiredCapacity)
        {
            return nativeSessionApi.GetHealthSnapshot(
                sessionHandle.Value,
                buffer,
                capacity,
                out requiredCapacity);
        }

        var healthPayload = ExecuteBufferOperationWithRetry(
            Invoke,
            HealthJsonBufferCapacity,
            "GetHealthSnapshot");
        var healthSnapshot = DeserializeRequired<BusHealthSnapshot>(
            healthPayload,
            "GetHealthSnapshot payload");

        return Task.FromResult(healthSnapshot);
    }

    /// <summary>
    /// 네이티브 세션의 최근 telemetry 이벤트를 조회합니다.
    /// </summary>
    /// <param name="sessionHandle">대상 세션 핸들입니다.</param>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>조회된 telemetry 이벤트 목록입니다.</returns>
    public Task<ReadOnlyCollection<TelemetryEventRecord>> PollTelemetryAsync(
        SessionHandle sessionHandle,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        int Invoke(StringBuilder buffer, int capacity, out int requiredCapacity)
        {
            return nativeSessionApi.PollTelemetry(
                sessionHandle.Value,
                buffer,
                capacity,
                out requiredCapacity);
        }

        var telemetryPayload = ExecuteBufferOperationWithRetry(
            Invoke,
            TelemetryJsonBufferCapacity,
            "PollTelemetry");
        var payloads = JsonSerializer.Deserialize<List<NativeTelemetryEventPayload>>(
            telemetryPayload,
            PayloadJsonOptions) ?? [];

        var events = payloads
            .Select(payload => new TelemetryEventRecord(
                payload.Type,
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
                        payload.MessageFrame.TimeTagMicros)))
            .ToList();

        return Task.FromResult(new ReadOnlyCollection<TelemetryEventRecord>(events));
    }

    private static string SerializeScenario(ScenarioDefinition scenario)
    {
        var payload = new NativeScenarioPayload(
            scenario.Name,
            scenario.ChannelId,
            scenario.ActiveBus,
            scenario.Schedules
                .Select(schedule => new NativeSchedulePayload(
                    schedule.Name,
                    schedule.PeriodMs,
                    schedule.RtAddress,
                    schedule.SubAddress,
                    schedule.Direction))
                .ToArray());

        return JsonSerializer.Serialize(payload, ScenarioJsonOptions);
    }

    private static T DeserializeRequired<T>(string payload, string label)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(payload, PayloadJsonOptions)
                ?? throw new InvalidOperationException($"{label}이 비어 있습니다.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"{label}을 해석하지 못했습니다. {exception.Message}", exception);
        }
    }

    private static string ExecuteBufferOperationWithRetry(
        NativeBufferOperation operation,
        int initialCapacity,
        string operationName)
    {
        var capacity = initialCapacity;

        for (var attempt = 0; attempt < MaxBufferResizeAttempts; attempt++)
        {
            var buffer = new StringBuilder(capacity);
            var status = operation(buffer, capacity, out var requiredCapacity);

            if (status == 0)
            {
                return buffer.ToString();
            }

            if (status != BufferTooSmallStatusCode)
            {
                EnsureSuccess(status, operationName);
            }

            capacity = GetNextBufferCapacity(capacity, requiredCapacity);
        }

        throw CreateBufferRetryLimitExceededException(operationName);
    }

    private static int GetNextBufferCapacity(int currentCapacity, int requiredCapacity)
    {
        if (requiredCapacity > currentCapacity)
        {
            return requiredCapacity;
        }

        return checked(currentCapacity * 2);
    }

    private static NativeInteropException CreateBufferRetryLimitExceededException(string operationName)
    {
        return new NativeInteropException(
            BufferTooSmallStatusCode,
            $"{operationName} 버퍼 재할당 재시도 한도를 초과했습니다.");
    }

    private static void EnsureSuccess(int statusCode, string operationName)
    {
        if (statusCode == 0)
        {
            return;
        }

        throw new NativeInteropException(
            statusCode,
            $"{operationName} 호출이 실패했습니다. status={statusCode}");
    }

    private sealed record NativeScenarioPayload(
        string Name,
        int ChannelId,
        BusLine ActiveBus,
        IReadOnlyList<NativeSchedulePayload> BcSchedules);

    private sealed record NativeSchedulePayload(
        string Name,
        int PeriodMs,
        int RtAddress,
        int SubAddress,
        TransferDirection Direction);

    private sealed record NativeTelemetryEventPayload(
        TelemetryEventType Type,
        long TimeTagMicros,
        BusLine ActiveBus,
        string Description,
        NativeTelemetryMessageFramePayload? MessageFrame);

    private sealed record NativeTelemetryMessageFramePayload(
        ushort CommandWordRaw,
        ushort? StatusWordRaw,
        List<ushort> DataWords,
        BusLine BusLine,
        long TimeTagMicros);
}
