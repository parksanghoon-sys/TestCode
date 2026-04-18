using MilStd1553.Cli.Contracts;
using MilStd1553.Cli.Models;
using MilStd1553.Host.Contracts;
using MilStd1553.Host.Models;

namespace MilStd1553.Cli.Services;

/// <summary>
/// 운영자용 CLI 진입 흐름을 오케스트레이션합니다.
/// </summary>
public sealed class HarnessCliRunner
{
    private readonly ISessionCoordinator sessionCoordinator;
    private readonly ITelemetryQueryService telemetryQueryService;
    private readonly IScenarioFileReader scenarioFileReader;
    private readonly TextWriter standardOutput;
    private readonly TextWriter standardError;

    /// <summary>
    /// CLI 러너를 생성합니다.
    /// </summary>
    /// <param name="sessionCoordinator">세션 제어 서비스입니다.</param>
    /// <param name="telemetryQueryService">텔레메트리 조회 서비스입니다.</param>
    /// <param name="scenarioFileReader">시나리오 파일 읽기 서비스입니다.</param>
    /// <param name="standardOutput">표준 출력 스트림입니다.</param>
    /// <param name="standardError">표준 오류 스트림입니다.</param>
    public HarnessCliRunner(
        ISessionCoordinator sessionCoordinator,
        ITelemetryQueryService telemetryQueryService,
        IScenarioFileReader scenarioFileReader,
        TextWriter standardOutput,
        TextWriter standardError)
    {
        this.sessionCoordinator = sessionCoordinator;
        this.telemetryQueryService = telemetryQueryService;
        this.scenarioFileReader = scenarioFileReader;
        this.standardOutput = standardOutput;
        this.standardError = standardError;
    }

    /// <summary>
    /// 전달된 인자를 해석해 CLI 작업을 실행합니다.
    /// </summary>
    /// <param name="args">명령줄 인자 목록입니다.</param>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>프로세스 종료 코드입니다.</returns>
    public async Task<int> RunAsync(
        IReadOnlyList<string> args,
        CancellationToken cancellationToken)
    {
        var parseResult = ParseOptions(args);
        if (!parseResult.IsSuccess)
        {
            await standardError.WriteLineAsync(parseResult.ErrorMessage).ConfigureAwait(false);
            await standardError.WriteLineAsync(BuildUsage()).ConfigureAwait(false);
            return 1;
        }

        var options = parseResult.Options!;
        if (options.ShowHelp)
        {
            await standardOutput.WriteLineAsync(BuildUsage()).ConfigureAwait(false);
            return 0;
        }

        var exitCode = 0;

        try
        {
            var scenarioJson = await scenarioFileReader
                .ReadAllTextAsync(options.ScenarioPath!, cancellationToken)
                .ConfigureAwait(false);

            var startResult = await sessionCoordinator
                .StartFromJsonAsync(scenarioJson, cancellationToken)
                .ConfigureAwait(false);

            await standardOutput.WriteLineAsync($"sessionHandle={startResult.SessionHandle.Value}").ConfigureAwait(false);
            await standardOutput.WriteLineAsync(FormatHealth("initialHealth", startResult.InitialHealth)).ConfigureAwait(false);

            if (options.SwitchBus is not null)
            {
                await sessionCoordinator
                    .SwitchBusAsync(options.SwitchBus.Value, cancellationToken)
                    .ConfigureAwait(false);

                var currentHealth = await sessionCoordinator
                    .GetHealthSnapshotAsync(cancellationToken)
                    .ConfigureAwait(false);

                await standardOutput.WriteLineAsync($"switchedBus={options.SwitchBus.Value}").ConfigureAwait(false);
                await standardOutput.WriteLineAsync(FormatHealth("currentHealth", currentHealth)).ConfigureAwait(false);
            }

            if (options.PollTelemetry)
            {
                var telemetryEvents = await telemetryQueryService
                    .PollTelemetryAsync(cancellationToken)
                    .ConfigureAwait(false);

                await standardOutput.WriteLineAsync($"telemetryCount={telemetryEvents.Count}").ConfigureAwait(false);

                for (var index = 0; index < telemetryEvents.Count; index++)
                {
                    await standardOutput
                        .WriteLineAsync(FormatTelemetry(index, telemetryEvents[index]))
                        .ConfigureAwait(false);
                }
            }
        }
        catch (Exception exception)
        {
            await standardError.WriteLineAsync($"실행 실패: {exception.Message}").ConfigureAwait(false);
            exitCode = 1;
        }
        finally
        {
            if (sessionCoordinator.HasActiveSession)
            {
                try
                {
                    await sessionCoordinator.StopAsync(cancellationToken).ConfigureAwait(false);
                    await standardOutput.WriteLineAsync("sessionStopped=true").ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    await standardError.WriteLineAsync($"세션 종료 실패: {exception.Message}").ConfigureAwait(false);
                    exitCode = 1;
                }
            }
        }

        return exitCode;
    }

    private static string BuildUsage()
    {
        return string.Join(
            Environment.NewLine,
            [
                "사용법:",
                "  MilStd1553.Cli --scenario <path> [--switch-bus A|B] [--poll-telemetry]",
                "옵션:",
                "  --scenario <path>      시나리오 JSON 파일 경로",
                "  --switch-bus A|B       세션 시작 후 지정한 Bus로 전환",
                "  --poll-telemetry       세션 종료 전 pending telemetry 출력",
                "  --help                 도움말 출력",
            ]);
    }

    private static string FormatHealth(
        string label,
        BusHealthSnapshot snapshot)
    {
        return $"{label} activeBus={snapshot.ActiveBus} standbyBus={snapshot.StandbyBus} timeoutCount={snapshot.TimeoutCount} retryCount={snapshot.RetryCount} degraded={snapshot.Degraded.ToString().ToLowerInvariant()}";
    }

    private static string FormatTelemetry(
        int index,
        TelemetryEventRecord record)
    {
        return $"telemetry[{index}] type={record.Type} activeBus={record.ActiveBus} timeTagMicros={record.TimeTagMicros} description={record.Description}";
    }

    private static ParseResult ParseOptions(IReadOnlyList<string> args)
    {
        string? scenarioPath = null;
        BusLine? switchBus = null;
        var pollTelemetry = false;
        var showHelp = false;

        for (var index = 0; index < args.Count; index++)
        {
            switch (args[index])
            {
                case "--help":
                case "-h":
                    showHelp = true;
                    break;

                case "--scenario":
                    if (!TryReadNextValue(args, ref index, out scenarioPath))
                    {
                        return ParseResult.Failure("--scenario 다음에 파일 경로가 필요합니다.");
                    }

                    break;

                case "--switch-bus":
                    if (!TryReadNextValue(args, ref index, out var busValue))
                    {
                        return ParseResult.Failure("--switch-bus 다음에 A 또는 B가 필요합니다.");
                    }

                    if (!Enum.TryParse<BusLine>(busValue, true, out var parsedBus))
                    {
                        return ParseResult.Failure("--switch-bus는 A 또는 B여야 합니다.");
                    }

                    switchBus = parsedBus;
                    break;

                case "--poll-telemetry":
                    pollTelemetry = true;
                    break;

                default:
                    return ParseResult.Failure($"알 수 없는 인자입니다: {args[index]}");
            }
        }

        if (showHelp)
        {
            return ParseResult.Success(new HarnessCliOptions(null, switchBus, pollTelemetry, true));
        }

        if (string.IsNullOrWhiteSpace(scenarioPath))
        {
            return ParseResult.Failure("--scenario는 필수입니다.");
        }

        return ParseResult.Success(new HarnessCliOptions(scenarioPath, switchBus, pollTelemetry, false));
    }

    private static bool TryReadNextValue(
        IReadOnlyList<string> args,
        ref int index,
        out string? value)
    {
        var nextIndex = index + 1;
        if (nextIndex >= args.Count || string.IsNullOrWhiteSpace(args[nextIndex]))
        {
            value = null;
            return false;
        }

        value = args[nextIndex];
        index = nextIndex;
        return true;
    }

    /// <summary>
    /// CLI 인자 파싱 결과를 표현합니다.
    /// </summary>
    /// <param name="IsSuccess">파싱 성공 여부입니다.</param>
    /// <param name="Options">파싱된 옵션입니다.</param>
    /// <param name="ErrorMessage">파싱 실패 메시지입니다.</param>
    private sealed record ParseResult(
        bool IsSuccess,
        HarnessCliOptions? Options,
        string? ErrorMessage)
    {
        /// <summary>
        /// 성공 결과를 생성합니다.
        /// </summary>
        /// <param name="options">파싱된 옵션입니다.</param>
        /// <returns>성공 결과입니다.</returns>
        public static ParseResult Success(HarnessCliOptions options)
        {
            return new ParseResult(true, options, null);
        }

        /// <summary>
        /// 실패 결과를 생성합니다.
        /// </summary>
        /// <param name="errorMessage">실패 메시지입니다.</param>
        /// <returns>실패 결과입니다.</returns>
        public static ParseResult Failure(string errorMessage)
        {
            return new ParseResult(false, null, errorMessage);
        }
    }
}
