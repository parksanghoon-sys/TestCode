using MilStd1553.Host.Models;

namespace MilStd1553.Host.Contracts;

/// <summary>
/// 시나리오 JSON을 Host 모델로 변환하는 계약입니다.
/// </summary>
public interface IScenarioDefinitionLoader
{
    /// <summary>
    /// 시나리오 JSON을 로드하고 검증합니다.
    /// </summary>
    /// <param name="scenarioJson">원본 시나리오 JSON입니다.</param>
    /// <returns>로딩 결과입니다.</returns>
    ScenarioLoadResult LoadFromJson(string scenarioJson);
}
