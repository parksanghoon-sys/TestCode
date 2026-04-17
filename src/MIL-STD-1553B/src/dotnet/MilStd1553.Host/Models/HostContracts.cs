using System.Collections.ObjectModel;

namespace MilStd1553.Host.Models;

/// <summary>
/// Host 계층에서 사용하는 버스 라인을 나타냅니다.
/// </summary>
public enum BusLine
{
    /// <summary>
    /// 주 버스 A입니다.
    /// </summary>
    A = 0,

    /// <summary>
    /// 예비 버스 B입니다.
    /// </summary>
    B = 1,
}

/// <summary>
/// Host 계층에서 사용하는 BC 스케줄 방향을 나타냅니다.
/// </summary>
public enum TransferDirection
{
    /// <summary>
    /// BC가 RT로 데이터를 보내는 수신 명령입니다.
    /// </summary>
    Receive = 0,

    /// <summary>
    /// BC가 RT로부터 데이터를 가져오는 송신 명령입니다.
    /// </summary>
    Transmit = 1,
}

/// <summary>
/// Host 관점의 버스 건강 상태를 표현합니다.
/// </summary>
/// <param name="ActiveBus">현재 활성 버스입니다.</param>
/// <param name="StandbyBus">현재 대기 버스입니다.</param>
/// <param name="TimeoutCount">누적 timeout 수입니다.</param>
/// <param name="RetryCount">누적 retry 수입니다.</param>
/// <param name="Degraded">degraded 상태 여부입니다.</param>
public sealed record BusHealthSnapshot(
    BusLine ActiveBus,
    BusLine StandbyBus,
    int TimeoutCount,
    int RetryCount,
    bool Degraded);

/// <summary>
/// BC 스케줄 항목을 표현합니다.
/// </summary>
/// <param name="Name">스케줄 이름입니다.</param>
/// <param name="PeriodMs">주기입니다.</param>
/// <param name="RtAddress">대상 RT 주소입니다.</param>
/// <param name="SubAddress">대상 서브어드레스입니다.</param>
/// <param name="Direction">전송 방향입니다.</param>
public sealed record ScenarioScheduleDefinition(
    string Name,
    int PeriodMs,
    int RtAddress,
    int SubAddress,
    TransferDirection Direction);

/// <summary>
/// Host가 다루는 시나리오 정의를 표현합니다.
/// </summary>
/// <param name="Name">시나리오 이름입니다.</param>
/// <param name="ChannelId">채널 식별자입니다.</param>
/// <param name="ActiveBus">초기 활성 버스입니다.</param>
/// <param name="Schedules">BC 스케줄 목록입니다.</param>
public sealed record ScenarioDefinition(
    string Name,
    int ChannelId,
    BusLine ActiveBus,
    ReadOnlyCollection<ScenarioScheduleDefinition> Schedules);

/// <summary>
/// 활성 세션을 식별하는 핸들입니다.
/// </summary>
/// <param name="Value">세션 식별자 문자열입니다.</param>
public sealed record SessionHandle(string Value);

/// <summary>
/// 세션 시작 결과를 표현합니다.
/// </summary>
/// <param name="SessionHandle">생성된 세션 핸들입니다.</param>
/// <param name="InitialHealth">초기 health snapshot입니다.</param>
public sealed record SessionStartResult(
    SessionHandle SessionHandle,
    BusHealthSnapshot InitialHealth);

/// <summary>
/// 시나리오 JSON 로딩 결과를 표현합니다.
/// </summary>
/// <param name="IsSuccess">성공 여부입니다.</param>
/// <param name="Scenario">성공 시 파싱된 시나리오입니다.</param>
/// <param name="Errors">실패 사유 목록입니다.</param>
public sealed record ScenarioLoadResult(
    bool IsSuccess,
    ScenarioDefinition? Scenario,
    ReadOnlyCollection<string> Errors)
{
    /// <summary>
    /// 성공 결과를 생성합니다.
    /// </summary>
    /// <param name="scenario">파싱된 시나리오입니다.</param>
    /// <returns>성공 결과입니다.</returns>
    public static ScenarioLoadResult Success(ScenarioDefinition scenario)
    {
        return new ScenarioLoadResult(true, scenario, new ReadOnlyCollection<string>([]));
    }

    /// <summary>
    /// 실패 결과를 생성합니다.
    /// </summary>
    /// <param name="errors">실패 사유 목록입니다.</param>
    /// <returns>실패 결과입니다.</returns>
    public static ScenarioLoadResult Failure(IEnumerable<string> errors)
    {
        return new ScenarioLoadResult(
            false,
            null,
            new ReadOnlyCollection<string>(errors.ToArray()));
    }
}
