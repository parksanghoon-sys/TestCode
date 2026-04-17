using MilStd1553.Host.Models;

namespace MilStd1553.Host.Contracts;

/// <summary>
/// Host에서 세션 시작, 중지, 버스 전환을 오케스트레이션하는 계약입니다.
/// </summary>
public interface ISessionCoordinator
{
    /// <summary>
    /// 현재 활성 세션 존재 여부를 반환합니다.
    /// </summary>
    bool HasActiveSession { get; }

    /// <summary>
    /// 현재 활성 세션 핸들을 반환합니다.
    /// </summary>
    SessionHandle? ActiveSessionHandle { get; }

    /// <summary>
    /// 시나리오 JSON으로부터 세션을 시작합니다.
    /// </summary>
    /// <param name="scenarioJson">시작할 시나리오 JSON입니다.</param>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>세션 시작 결과입니다.</returns>
    Task<SessionStartResult> StartFromJsonAsync(
        string scenarioJson,
        CancellationToken cancellationToken);

    /// <summary>
    /// 현재 활성 세션을 종료합니다.
    /// </summary>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>비동기 작업입니다.</returns>
    Task StopAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 현재 활성 세션의 버스를 전환합니다.
    /// </summary>
    /// <param name="busLine">전환할 버스입니다.</param>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>비동기 작업입니다.</returns>
    Task SwitchBusAsync(
        BusLine busLine,
        CancellationToken cancellationToken);

    /// <summary>
    /// 현재 활성 세션의 health snapshot을 조회합니다.
    /// </summary>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>조회된 health snapshot입니다.</returns>
    Task<BusHealthSnapshot> GetHealthSnapshotAsync(CancellationToken cancellationToken);
}
