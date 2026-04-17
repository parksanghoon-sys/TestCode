using System.Collections.ObjectModel;
using System.Text.Json;
using MilStd1553.Host.Contracts;
using MilStd1553.Host.Models;

namespace MilStd1553.Host.Services;

/// <summary>
/// 시나리오 JSON을 Host 모델로 읽고 기본 규칙을 검증합니다.
/// </summary>
public sealed class ScenarioDefinitionJsonLoader : IScenarioDefinitionLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// 시나리오 JSON을 로드하고 검증합니다.
    /// </summary>
    /// <param name="scenarioJson">원본 시나리오 JSON입니다.</param>
    /// <returns>로딩 결과입니다.</returns>
    public ScenarioLoadResult LoadFromJson(string scenarioJson)
    {
        if (string.IsNullOrWhiteSpace(scenarioJson))
        {
            return ScenarioLoadResult.Failure(["시나리오 JSON이 비어 있습니다."]);
        }

        RawScenarioDefinition? rawScenario;
        try
        {
            rawScenario = JsonSerializer.Deserialize<RawScenarioDefinition>(scenarioJson, JsonOptions);
        }
        catch (JsonException exception)
        {
            return ScenarioLoadResult.Failure([$"JSON 파싱에 실패했습니다: {exception.Message}"]);
        }

        if (rawScenario is null)
        {
            return ScenarioLoadResult.Failure(["시나리오 JSON을 해석하지 못했습니다."]);
        }

        var errors = new List<string>();

        if (!TryParseBusLine(rawScenario.ActiveBus, out var activeBus))
        {
            errors.Add("activeBus는 A 또는 B여야 합니다.");
        }

        if (rawScenario.ChannelId < 0)
        {
            errors.Add("channelId는 0 이상이어야 합니다.");
        }

        if (rawScenario.BcSchedules is null || rawScenario.BcSchedules.Count == 0)
        {
            errors.Add("bcSchedules는 최소 1개 이상이어야 합니다.");
        }

        var schedules = new List<ScenarioScheduleDefinition>();
        if (rawScenario.BcSchedules is not null)
        {
            for (var index = 0; index < rawScenario.BcSchedules.Count; index++)
            {
                var schedule = rawScenario.BcSchedules[index];
                ValidateSchedule(schedule, index, schedules, errors);
            }
        }

        if (errors.Count > 0)
        {
            return ScenarioLoadResult.Failure(errors);
        }

        var scenario = new ScenarioDefinition(
            string.IsNullOrWhiteSpace(rawScenario.Name) ? "기본시나리오" : rawScenario.Name.Trim(),
            rawScenario.ChannelId,
            activeBus,
            new ReadOnlyCollection<ScenarioScheduleDefinition>(schedules));

        return ScenarioLoadResult.Success(scenario);
    }

    private static bool TryParseBusLine(string? value, out BusLine busLine)
    {
        return Enum.TryParse(value, true, out busLine);
    }

    private static void ValidateSchedule(
        RawScheduleDefinition schedule,
        int index,
        ICollection<ScenarioScheduleDefinition> schedules,
        ICollection<string> errors)
    {
        var prefix = $"bcSchedules[{index}]";
        if (string.IsNullOrWhiteSpace(schedule.Name))
        {
            errors.Add($"{prefix}.name은 필수입니다.");
        }

        if (schedule.PeriodMs <= 0)
        {
            errors.Add($"{prefix}.periodMs는 1 이상이어야 합니다.");
        }

        if (schedule.RtAddress is < 0 or > 30)
        {
            errors.Add($"{prefix}.rtAddress는 0에서 30 사이여야 합니다.");
        }

        if (schedule.SubAddress is < 1 or > 30)
        {
            errors.Add($"{prefix}.subAddress는 1에서 30 사이여야 합니다.");
        }

        if (!Enum.TryParse<TransferDirection>(schedule.Direction, true, out var direction))
        {
            errors.Add($"{prefix}.direction은 Receive 또는 Transmit이어야 합니다.");
            return;
        }

        if (errors.Any(error => error.StartsWith(prefix, StringComparison.Ordinal)))
        {
            return;
        }

        schedules.Add(new ScenarioScheduleDefinition(
            schedule.Name!.Trim(),
            schedule.PeriodMs,
            schedule.RtAddress,
            schedule.SubAddress,
            direction));
    }

    private sealed class RawScenarioDefinition
    {
        public string? Name { get; init; }

        public int ChannelId { get; init; }

        public string? ActiveBus { get; init; }

        public List<RawScheduleDefinition>? BcSchedules { get; init; }
    }

    private sealed class RawScheduleDefinition
    {
        public string? Name { get; init; }

        public int PeriodMs { get; init; }

        public int RtAddress { get; init; }

        public int SubAddress { get; init; }

        public string? Direction { get; init; }
    }
}
