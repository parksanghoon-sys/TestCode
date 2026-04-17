using System.Globalization;

namespace Mes.MockStation.Example;

/// <summary>
/// example 폴더용 mock station 시나리오를 생성하는 콘솔 진입점입니다.
/// </summary>
public static class Program
{
    /// <summary>
    /// SQLite mock scenario를 생성하고 결과 요약을 콘솔에 출력합니다.
    /// </summary>
    /// <param name="args">`--output-root`와 선택적 `--seeded-at` 인자입니다.</param>
    /// <returns>성공 시 0을 반환합니다.</returns>
    public static int Main(string[] args)
    {
        var options = ParseArguments(args);
        var request = new MockOperatorExecutionSeedRequest
        {
            DatabaseFilePath = Path.Combine(options.OutputRoot, "operator-execution-example.db"),
            ManifestFilePath = Path.Combine(options.OutputRoot, "mock-station-scenario.json"),
            SeededAt = options.SeededAt
        };

        var result = MockOperatorExecutionScenarioSeeder.SeedSqlite(request);
        WriteSummary(result.Manifest);
        return 0;
    }

    /// <summary>
    /// 콘솔 인자를 파싱해 output root와 기준 시각을 결정합니다.
    /// </summary>
    /// <param name="args">원본 콘솔 인자입니다.</param>
    /// <returns>정규화된 예제 생성 옵션입니다.</returns>
    private static MockOperatorExecutionProgramOptions ParseArguments(string[] args)
    {
        var outputRoot = Path.Combine(Environment.CurrentDirectory, ".runtime");
        var seededAt = new DateTimeOffset(2026, 4, 17, 13, 0, 0, TimeSpan.Zero);

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--output-root":
                    outputRoot = RequireValue(args, ++index, "--output-root");
                    break;
                case "--seeded-at":
                    seededAt = DateTimeOffset.Parse(
                        RequireValue(args, ++index, "--seeded-at"),
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind);
                    break;
                default:
                    throw new ArgumentException($"Unsupported argument '{args[index]}'.");
            }
        }

        return new MockOperatorExecutionProgramOptions
        {
            OutputRoot = Path.GetFullPath(outputRoot),
            SeededAt = seededAt
        };
    }

    /// <summary>
    /// 값이 필요한 콘솔 옵션의 다음 인자를 읽습니다.
    /// </summary>
    /// <param name="args">원본 콘솔 인자 배열입니다.</param>
    /// <param name="index">읽을 값 인덱스입니다.</param>
    /// <param name="optionName">오류 메시지에 사용할 옵션 이름입니다.</param>
    /// <returns>해당 옵션의 값입니다.</returns>
    private static string RequireValue(string[] args, int index, string optionName)
    {
        if (index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
        {
            throw new ArgumentException($"Option '{optionName}' requires a value.");
        }

        return args[index];
    }

    /// <summary>
    /// 생성된 mock station 시나리오 요약을 콘솔에 출력합니다.
    /// </summary>
    /// <param name="manifest">출력할 시나리오 manifest입니다.</param>
    private static void WriteSummary(MockOperatorExecutionScenarioManifest manifest)
    {
        Console.WriteLine("Mock station scenario is ready.");
        Console.WriteLine($"Scenario: {manifest.ScenarioName}");
        Console.WriteLine($"Station: {manifest.StationId}");
        Console.WriteLine($"Actor : {manifest.DefaultActorId}");
        Console.WriteLine($"DB    : {manifest.DatabaseFilePath}");
        Console.WriteLine($"JSON  : {manifest.ManifestFilePath}");
        Console.WriteLine($"Start demo operation   : {manifest.QueuedOperation.OperationExecutionId}");
        Console.WriteLine($"Running demo operation : {manifest.RunningOperation.OperationExecutionId}");
        Console.WriteLine($"Running demo WIP       : {manifest.RunningOperation.WipUnitId}");
        Console.WriteLine($"Running demo lot       : {manifest.RunningOperation.MaterialLotId}");
    }

    /// <summary>
    /// 콘솔 진입점이 사용하는 정규화된 옵션입니다.
    /// </summary>
    private sealed record MockOperatorExecutionProgramOptions
    {
        /// <summary>
        /// SQLite와 manifest를 생성할 출력 루트 디렉터리입니다.
        /// </summary>
        public string OutputRoot { get; init; } = string.Empty;

        /// <summary>
        /// 시나리오 생성 기준 시각입니다.
        /// </summary>
        public DateTimeOffset SeededAt { get; init; }
    }
}
