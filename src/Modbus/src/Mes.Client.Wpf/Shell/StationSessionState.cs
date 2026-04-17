namespace Mes.Client.Wpf.Shell;

/// <summary>
/// 현재 바인딩된 스테이션 세션 정보를 나타냅니다.
/// </summary>
/// <param name="StationId">바인딩된 스테이션 식별자입니다.</param>
/// <param name="BoundAt">세션이 바인딩된 시각입니다.</param>
public sealed record StationSessionState(
    string StationId,
    DateTimeOffset BoundAt)
{
    /// <summary>
    /// 스테이션 식별자를 정규화해 세션 상태를 생성합니다.
    /// </summary>
    /// <param name="stationId">바인딩할 스테이션 식별자입니다.</param>
    /// <param name="boundAt">바인딩 시각입니다.</param>
    /// <returns>정규화된 세션 상태입니다.</returns>
    public static StationSessionState Bind(string stationId, DateTimeOffset boundAt)
    {
        var normalizedStationId = stationId?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedStationId))
        {
            throw new ArgumentException("StationId must not be blank.", nameof(stationId));
        }

        return new StationSessionState(normalizedStationId, boundAt);
    }
}
