using MilStd1553.Host.Contracts;
using MilStd1553.Host.Exceptions;
using MilStd1553.Host.Models;

namespace MilStd1553.Host.Services;

/// <summary>
/// Host 단계에서 시나리오 검증과 네이티브 세션 제어를 오케스트레이션합니다.
/// </summary>
public sealed class SessionCoordinator : ISessionCoordinator
{
    private readonly IScenarioDefinitionLoader scenarioDefinitionLoader;
    private readonly INativeHarnessClient nativeHarnessClient;
    private SessionHandle? activeSessionHandle;

    /// <summary>
    /// 세션 코디네이터를 생성합니다.
    /// </summary>
    /// <param name="scenarioDefinitionLoader">시나리오 로더입니다.</param>
    /// <param name="nativeHarnessClient">네이티브 하네스 클라이언트입니다.</param>
    public SessionCoordinator(
        IScenarioDefinitionLoader scenarioDefinitionLoader,
        INativeHarnessClient nativeHarnessClient)
    {
        this.scenarioDefinitionLoader = scenarioDefinitionLoader;
        this.nativeHarnessClient = nativeHarnessClient;
    }

    /// <summary>
    /// 현재 활성 세션 존재 여부를 반환합니다.
    /// </summary>
    public bool HasActiveSession => activeSessionHandle is not null;

    /// <summary>
    /// 현재 활성 세션 핸들을 반환합니다.
    /// </summary>
    public SessionHandle? ActiveSessionHandle => activeSessionHandle;

    /// <summary>
    /// 시나리오 JSON으로부터 세션을 시작합니다.
    /// </summary>
    /// <param name="scenarioJson">시작할 시나리오 JSON입니다.</param>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>세션 시작 결과입니다.</returns>
    public async Task<SessionStartResult> StartFromJsonAsync(
        string scenarioJson,
        CancellationToken cancellationToken)
    {
        if (activeSessionHandle is not null)
        {
            throw new InvalidOperationException("이미 활성 세션이 존재합니다.");
        }

        var loadResult = scenarioDefinitionLoader.LoadFromJson(scenarioJson);
        if (!loadResult.IsSuccess || loadResult.Scenario is null)
        {
            throw new ScenarioValidationException(loadResult.Errors);
        }

        var startResult = await nativeHarnessClient
            .OpenSessionAsync(loadResult.Scenario, cancellationToken)
            .ConfigureAwait(false);

        activeSessionHandle = startResult.SessionHandle;
        return startResult;
    }

    /// <summary>
    /// 현재 활성 세션을 종료합니다.
    /// </summary>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>비동기 작업입니다.</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (activeSessionHandle is null)
        {
            return;
        }

        var sessionHandle = activeSessionHandle;
        await nativeHarnessClient
            .StopSessionAsync(sessionHandle, cancellationToken)
            .ConfigureAwait(false);

        activeSessionHandle = null;
    }

    /// <summary>
    /// 현재 활성 세션의 버스를 전환합니다.
    /// </summary>
    /// <param name="busLine">전환할 버스입니다.</param>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>비동기 작업입니다.</returns>
    public Task SwitchBusAsync(
        BusLine busLine,
        CancellationToken cancellationToken)
    {
        return nativeHarnessClient.SwitchBusAsync(
            GetRequiredSessionHandle(),
            busLine,
            cancellationToken);
    }

    /// <summary>
    /// 현재 활성 세션의 health snapshot을 조회합니다.
    /// </summary>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>조회된 health snapshot입니다.</returns>
    public Task<BusHealthSnapshot> GetHealthSnapshotAsync(CancellationToken cancellationToken)
    {
        return nativeHarnessClient.GetHealthSnapshotAsync(
            GetRequiredSessionHandle(),
            cancellationToken);
    }

    private SessionHandle GetRequiredSessionHandle()
    {
        return activeSessionHandle
            ?? throw new InvalidOperationException("활성 세션이 없습니다.");
    }
}
