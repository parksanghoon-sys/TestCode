using MilStd1553.Host.Models;
using MilStd1553.Host.Services;

namespace MilStd1553.Host.Tests;

/// <summary>
/// 시나리오 JSON 로더를 검증합니다.
/// </summary>
public sealed class ScenarioDefinitionJsonLoaderTests
{
    /// <summary>
    /// 잘못된 버스 값과 빈 스케줄이 있으면 실패해야 합니다.
    /// </summary>
    [Fact]
    public void LoadFromJson_InvalidScenario_ReturnsErrors()
    {
        const string scenarioJson = """
            {
              "channelId": 0,
              "activeBus": "C",
              "bcSchedules": []
            }
            """;

        var loader = new ScenarioDefinitionJsonLoader();

        var result = loader.LoadFromJson(scenarioJson);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Contains("activeBus", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error => error.Contains("bcSchedules", StringComparison.Ordinal));
    }

    /// <summary>
    /// 정상 JSON은 Host 시나리오 모델로 변환되어야 합니다.
    /// </summary>
    [Fact]
    public void LoadFromJson_ValidScenario_ReturnsScenario()
    {
        const string scenarioJson = """
            {
              "name": "기본상태조회",
              "channelId": 2,
              "activeBus": "B",
              "bcSchedules": [
                {
                  "name": "PollRt1Status",
                  "periodMs": 20,
                  "rtAddress": 1,
                  "subAddress": 2,
                  "direction": "Receive"
                }
              ]
            }
            """;

        var loader = new ScenarioDefinitionJsonLoader();

        var result = loader.LoadFromJson(scenarioJson);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Scenario);
        Assert.Equal("기본상태조회", result.Scenario!.Name);
        Assert.Equal(BusLine.B, result.Scenario.ActiveBus);
        Assert.Single(result.Scenario.Schedules);
    }
}
