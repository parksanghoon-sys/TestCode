using System.Collections.ObjectModel;
using System.Text;
using MilStd1553.Cli.Contracts;
using MilStd1553.Cli.Services;
using MilStd1553.Host.Contracts;
using MilStd1553.Host.Models;

namespace MilStd1553.Host.Tests;

/// <summary>
/// CLI baseline 실행 흐름을 검증합니다.
/// </summary>
public sealed class HarnessCliRunnerTests
{
    /// <summary>
    /// 도움말 요청 시 사용법을 출력하고 성공 코드로 종료해야 합니다.
    /// </summary>
    [Fact]
    public async Task RunAsync_WhenHelpRequested_WritesUsageAndReturnsZero()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var runner = CreateRunner(
            new FakeSessionCoordinator(),
            new FakeTelemetryQueryService(new ReadOnlyCollection<TelemetryEventRecord>([])),
            new FakeScenarioFileReader(),
            output,
            error);

        var exitCode = await runner.RunAsync(["--help"], CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Contains("사용법:", output.ToString(), StringComparison.Ordinal);
        Assert.Equal(string.Empty, error.ToString());
    }

    /// <summary>
    /// 시나리오 인자가 없으면 오류와 사용법을 출력해야 합니다.
    /// </summary>
    [Fact]
    public async Task RunAsync_WhenScenarioIsMissing_WritesErrorAndReturnsOne()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var runner = CreateRunner(
            new FakeSessionCoordinator(),
            new FakeTelemetryQueryService(new ReadOnlyCollection<TelemetryEventRecord>([])),
            new FakeScenarioFileReader(),
            output,
            error);

        var exitCode = await runner.RunAsync(["--poll-telemetry"], CancellationToken.None);

        Assert.Equal(1, exitCode);
        Assert.Contains("--scenario는 필수입니다.", error.ToString(), StringComparison.Ordinal);
        Assert.Equal(string.Empty, output.ToString());
    }

    /// <summary>
    /// 시나리오 파일 읽기부터 세션 종료까지 한 번의 실행 흐름으로 이어져야 합니다.
    /// </summary>
    [Fact]
    public async Task RunAsync_WithScenarioSwitchAndTelemetry_ExecutesFullLifecycle()
    {
        const string scenarioPath = "scenario.json";
        const string scenarioJson = """{"name":"CliScenario","channelId":0,"activeBus":"A","bcSchedules":[{"name":"PollRt1","periodMs":20,"rtAddress":1,"subAddress":2,"direction":"Receive"}]}""";

        var coordinator = new FakeSessionCoordinator
        {
            StartResult = new SessionStartResult(
                new SessionHandle("session-0099"),
                new BusHealthSnapshot(BusLine.A, BusLine.B, 0, 0, false)),
            CurrentHealth = new BusHealthSnapshot(BusLine.B, BusLine.A, 1, 1, true),
        };
        var telemetryEvents = new ReadOnlyCollection<TelemetryEventRecord>(
        [
            new TelemetryEventRecord(
                TelemetryEventType.BusSwitch,
                125,
                BusLine.B,
                "수동 버스 전환",
                null),
        ]);
        var output = new StringWriter(new StringBuilder());
        var error = new StringWriter(new StringBuilder());
        var runner = CreateRunner(
            coordinator,
            new FakeTelemetryQueryService(telemetryEvents),
            new FakeScenarioFileReader
            {
                Contents =
                {
                    [scenarioPath] = scenarioJson,
                },
            },
            output,
            error);

        var exitCode = await runner.RunAsync(
            ["--scenario", scenarioPath, "--switch-bus", "B", "--poll-telemetry"],
            CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Equal(scenarioJson, coordinator.LastScenarioJson);
        Assert.Equal(1, coordinator.StartCallCount);
        Assert.Equal(BusLine.B, coordinator.LastSwitchedBus);
        Assert.Equal(1, coordinator.StopCallCount);
        Assert.Contains("sessionHandle=session-0099", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("initialHealth activeBus=A", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("switchedBus=B", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("currentHealth activeBus=B", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("telemetryCount=1", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("telemetry[0] type=BusSwitch", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("sessionStopped=true", output.ToString(), StringComparison.Ordinal);
        Assert.Equal(string.Empty, error.ToString());
    }

    private static HarnessCliRunner CreateRunner(
        ISessionCoordinator sessionCoordinator,
        ITelemetryQueryService telemetryQueryService,
        IScenarioFileReader scenarioFileReader,
        TextWriter standardOutput,
        TextWriter standardError)
    {
        return new HarnessCliRunner(
            sessionCoordinator,
            telemetryQueryService,
            scenarioFileReader,
            standardOutput,
            standardError);
    }

    /// <summary>
    /// 테스트용 시나리오 파일 읽기 구현입니다.
    /// </summary>
    private sealed class FakeScenarioFileReader : IScenarioFileReader
    {
        /// <summary>
        /// 경로별 파일 본문 맵입니다.
        /// </summary>
        public Dictionary<string, string> Contents { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 지정한 경로의 본문을 반환합니다.
        /// </summary>
        /// <param name="path">파일 경로입니다.</param>
        /// <param name="cancellationToken">취소 토큰입니다.</param>
        /// <returns>파일 본문입니다.</returns>
        public Task<string> ReadAllTextAsync(
            string path,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Contents.TryGetValue(path, out var content))
            {
                throw new FileNotFoundException("테스트용 시나리오 파일을 찾을 수 없습니다.", path);
            }

            return Task.FromResult(content);
        }
    }

    /// <summary>
    /// 테스트용 세션 코디네이터입니다.
    /// </summary>
    private sealed class FakeSessionCoordinator : ISessionCoordinator
    {
        private SessionHandle? activeSessionHandle;

        /// <summary>
        /// 시작 결과입니다.
        /// </summary>
        public SessionStartResult StartResult { get; init; } = new(
            new SessionHandle("session-0001"),
            new BusHealthSnapshot(BusLine.A, BusLine.B, 0, 0, false));

        /// <summary>
        /// 현재 health snapshot입니다.
        /// </summary>
        public BusHealthSnapshot CurrentHealth { get; set; } = new(BusLine.A, BusLine.B, 0, 0, false);

        /// <summary>
        /// 마지막 시작 시나리오 JSON입니다.
        /// </summary>
        public string? LastScenarioJson { get; private set; }

        /// <summary>
        /// 시작 호출 횟수입니다.
        /// </summary>
        public int StartCallCount { get; private set; }

        /// <summary>
        /// 종료 호출 횟수입니다.
        /// </summary>
        public int StopCallCount { get; private set; }

        /// <summary>
        /// 마지막 Bus 전환 값입니다.
        /// </summary>
        public BusLine? LastSwitchedBus { get; private set; }

        /// <summary>
        /// 활성 세션 존재 여부입니다.
        /// </summary>
        public bool HasActiveSession => activeSessionHandle is not null;

        /// <summary>
        /// 현재 활성 세션 핸들입니다.
        /// </summary>
        public SessionHandle? ActiveSessionHandle => activeSessionHandle;

        /// <summary>
        /// 시나리오 JSON으로 세션을 시작합니다.
        /// </summary>
        /// <param name="scenarioJson">시나리오 JSON입니다.</param>
        /// <param name="cancellationToken">취소 토큰입니다.</param>
        /// <returns>시작 결과입니다.</returns>
        public Task<SessionStartResult> StartFromJsonAsync(
            string scenarioJson,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StartCallCount++;
            LastScenarioJson = scenarioJson;
            activeSessionHandle = StartResult.SessionHandle;
            return Task.FromResult(StartResult);
        }

        /// <summary>
        /// 세션을 종료합니다.
        /// </summary>
        /// <param name="cancellationToken">취소 토큰입니다.</param>
        /// <returns>비동기 작업입니다.</returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StopCallCount++;
            activeSessionHandle = null;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Bus를 전환합니다.
        /// </summary>
        /// <param name="busLine">전환할 Bus입니다.</param>
        /// <param name="cancellationToken">취소 토큰입니다.</param>
        /// <returns>비동기 작업입니다.</returns>
        public Task SwitchBusAsync(
            BusLine busLine,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastSwitchedBus = busLine;
            return Task.CompletedTask;
        }

        /// <summary>
        /// 현재 health snapshot을 반환합니다.
        /// </summary>
        /// <param name="cancellationToken">취소 토큰입니다.</param>
        /// <returns>health snapshot입니다.</returns>
        public Task<BusHealthSnapshot> GetHealthSnapshotAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(CurrentHealth);
        }
    }

    /// <summary>
    /// 테스트용 텔레메트리 조회 서비스입니다.
    /// </summary>
    private sealed class FakeTelemetryQueryService : ITelemetryQueryService
    {
        private readonly ReadOnlyCollection<TelemetryEventRecord> telemetryEvents;

        /// <summary>
        /// 테스트용 서비스를 생성합니다.
        /// </summary>
        /// <param name="telemetryEvents">반환할 텔레메트리 목록입니다.</param>
        public FakeTelemetryQueryService(ReadOnlyCollection<TelemetryEventRecord> telemetryEvents)
        {
            this.telemetryEvents = telemetryEvents;
        }

        /// <summary>
        /// 텔레메트리 목록을 반환합니다.
        /// </summary>
        /// <param name="cancellationToken">취소 토큰입니다.</param>
        /// <returns>텔레메트리 목록입니다.</returns>
        public Task<ReadOnlyCollection<TelemetryEventRecord>> PollTelemetryAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(telemetryEvents);
        }
    }
}
