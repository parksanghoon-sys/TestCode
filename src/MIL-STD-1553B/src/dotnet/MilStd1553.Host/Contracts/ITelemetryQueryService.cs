using System.Collections.ObjectModel;
using MilStd1553.Host.Models;

namespace MilStd1553.Host.Contracts;

/// <summary>
/// Host에서 최근 telemetry 이벤트를 조회하는 계약입니다.
/// </summary>
public interface ITelemetryQueryService
{
    /// <summary>
    /// 현재 활성 세션의 telemetry 이벤트를 조회합니다.
    /// </summary>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>최근 telemetry 이벤트 목록입니다.</returns>
    Task<ReadOnlyCollection<TelemetryEventRecord>> PollTelemetryAsync(
        CancellationToken cancellationToken);
}
