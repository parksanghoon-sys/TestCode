using System.Collections.ObjectModel;
using MilStd1553.Host.Contracts;
using MilStd1553.Host.Models;

namespace MilStd1553.Host.Services;

/// <summary>
/// 현재 활성 세션의 telemetry 조회를 오케스트레이션합니다.
/// </summary>
public sealed class TelemetryQueryService : ITelemetryQueryService
{
    private readonly ISessionCoordinator sessionCoordinator;
    private readonly INativeHarnessClient nativeHarnessClient;

    /// <summary>
    /// telemetry 조회 서비스를 생성합니다.
    /// </summary>
    /// <param name="sessionCoordinator">활성 세션 상태를 제공하는 코디네이터입니다.</param>
    /// <param name="nativeHarnessClient">네이티브 하네스 클라이언트입니다.</param>
    public TelemetryQueryService(
        ISessionCoordinator sessionCoordinator,
        INativeHarnessClient nativeHarnessClient)
    {
        this.sessionCoordinator = sessionCoordinator;
        this.nativeHarnessClient = nativeHarnessClient;
    }

    /// <summary>
    /// 현재 활성 세션의 telemetry 이벤트를 조회합니다.
    /// </summary>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>최근 telemetry 이벤트 목록입니다.</returns>
    public Task<ReadOnlyCollection<TelemetryEventRecord>> PollTelemetryAsync(
        CancellationToken cancellationToken)
    {
        var sessionHandle = sessionCoordinator.ActiveSessionHandle
            ?? throw new InvalidOperationException("활성 세션이 없습니다.");

        return nativeHarnessClient.PollTelemetryAsync(sessionHandle, cancellationToken);
    }
}
