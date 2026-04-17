using System.Collections.ObjectModel;
using MilStd1553.Host.Models;

namespace MilStd1553.Host.Contracts;

/// <summary>
/// C# Host에서 네이티브 하네스를 호출하는 최소 계약입니다.
/// </summary>
public interface INativeHarnessClient
{
    /// <summary>
    /// 네이티브 세션을 시작합니다.
    /// </summary>
    /// <param name="scenario">시작할 시나리오입니다.</param>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>세션 시작 결과입니다.</returns>
    Task<SessionStartResult> OpenSessionAsync(
        ScenarioDefinition scenario,
        CancellationToken cancellationToken);

    /// <summary>
    /// 네이티브 세션을 종료합니다.
    /// </summary>
    /// <param name="sessionHandle">종료할 세션 핸들입니다.</param>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>비동기 작업입니다.</returns>
    Task StopSessionAsync(
        SessionHandle sessionHandle,
        CancellationToken cancellationToken);

    /// <summary>
    /// 네이티브 세션의 활성 버스를 전환합니다.
    /// </summary>
    /// <param name="sessionHandle">대상 세션 핸들입니다.</param>
    /// <param name="busLine">전환할 버스입니다.</param>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>비동기 작업입니다.</returns>
    Task SwitchBusAsync(
        SessionHandle sessionHandle,
        BusLine busLine,
        CancellationToken cancellationToken);

    /// <summary>
    /// 네이티브 세션의 최신 health snapshot을 조회합니다.
    /// </summary>
    /// <param name="sessionHandle">대상 세션 핸들입니다.</param>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>조회된 health snapshot입니다.</returns>
    Task<BusHealthSnapshot> GetHealthSnapshotAsync(
        SessionHandle sessionHandle,
        CancellationToken cancellationToken);

    /// <summary>
    /// 네이티브 세션의 최근 telemetry 이벤트를 조회합니다.
    /// </summary>
    /// <param name="sessionHandle">대상 세션 핸들입니다.</param>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>조회된 telemetry 이벤트 목록입니다.</returns>
    Task<ReadOnlyCollection<TelemetryEventRecord>> PollTelemetryAsync(
        SessionHandle sessionHandle,
        CancellationToken cancellationToken);
}
