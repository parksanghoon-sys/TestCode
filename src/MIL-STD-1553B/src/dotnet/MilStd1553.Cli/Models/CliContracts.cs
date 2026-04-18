using MilStd1553.Host.Models;

namespace MilStd1553.Cli.Models;

/// <summary>
/// CLI 실행 옵션을 표현합니다.
/// </summary>
/// <param name="ScenarioPath">시나리오 파일 경로입니다.</param>
/// <param name="SwitchBus">실행 중 전환할 Bus 옵션입니다.</param>
/// <param name="PollTelemetry">텔레메트리 조회 여부입니다.</param>
/// <param name="ShowHelp">도움말 출력 여부입니다.</param>
public sealed record HarnessCliOptions(
    string? ScenarioPath,
    BusLine? SwitchBus,
    bool PollTelemetry,
    bool ShowHelp);
