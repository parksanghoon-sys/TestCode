using Mes.Client.Wpf.Shell;

namespace Mes.Client.Wpf.Tests;

/// <summary>
/// 스테이션 세션 상태 규칙을 검증합니다.
/// </summary>
public sealed class StationSessionStateTests
{
    /// <summary>
    /// 바인딩 시 스테이션 식별자 앞뒤 공백을 제거하는지 확인합니다.
    /// </summary>
    [Fact]
    public void Bind_WhenStationIdHasPadding_TrimsAndCreatesSession()
    {
        var boundAt = new DateTimeOffset(2026, 4, 17, 10, 30, 0, TimeSpan.Zero);

        var session = StationSessionState.Bind("  ST-1001  ", boundAt);

        Assert.Equal("ST-1001", session.StationId);
        Assert.Equal(boundAt, session.BoundAt);
    }

    /// <summary>
    /// 빈 식별자로 바인딩하면 예외를 던지는지 확인합니다.
    /// </summary>
    [Fact]
    public void Bind_WhenStationIdIsBlank_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => StationSessionState.Bind("   ", DateTimeOffset.UtcNow));
    }
}
