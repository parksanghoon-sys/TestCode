using System.Diagnostics;

namespace MilStd1553.Host.Tests;

/// <summary>
/// 실장비 없이 simulator 경로로 동작하는 mock CLI example smoke test를 검증합니다.
/// </summary>
[Collection("Native runtime loader")]
public sealed class MockCliExampleSmokeTests
{
    /// <summary>
    /// example PowerShell 스크립트가 publish와 실행, 출력 검증까지 성공해야 합니다.
    /// </summary>
    [Fact]
    public void RunMockCliExampleScript_CompletesPublishRunAndVerification()
    {
        using var outputScope = ExampleOutputScope.Create();
        var repositoryRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(
            repositoryRoot.FullName,
            "examples",
            "mock-cli",
            "run_mock_cli_example.ps1");

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -OutputRoot \"{outputScope.OutputPath}\"",
            WorkingDirectory = repositoryRoot.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("mock example 스크립트 프로세스를 시작하지 못했습니다.");

        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.True(
            process.ExitCode == 0,
            $"mock example 스크립트가 실패했습니다.{Environment.NewLine}{standardOutput}{Environment.NewLine}{standardError}");

        var logPath = Path.Combine(outputScope.OutputPath, "mock-cli-output.log");
        Assert.True(File.Exists(logPath), "mock example 실행 로그 파일이 생성되어야 합니다.");

        var logContents = File.ReadAllText(logPath);
        Assert.Contains("sessionHandle=", logContents, StringComparison.Ordinal);
        Assert.Contains("initialHealth activeBus=A", logContents, StringComparison.Ordinal);
        Assert.Contains("switchedBus=B", logContents, StringComparison.Ordinal);
        Assert.Contains("currentHealth activeBus=B", logContents, StringComparison.Ordinal);
        Assert.Contains("telemetryCount=2", logContents, StringComparison.Ordinal);
        Assert.Contains("telemetry[0] type=MessageFrame", logContents, StringComparison.Ordinal);
        Assert.Contains("telemetry[1] type=BusSwitch", logContents, StringComparison.Ordinal);
        Assert.Contains("sessionStopped=true", logContents, StringComparison.Ordinal);
    }

    /// <summary>
    /// 저장소 루트를 탐색합니다.
    /// </summary>
    /// <returns>저장소 루트 디렉터리입니다.</returns>
    private static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AGENT_RULES.md")))
            {
                return current;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("저장소 루트를 찾지 못했습니다.");
    }

    /// <summary>
    /// mock example 검증용 임시 출력 루트를 관리합니다.
    /// </summary>
    private sealed class ExampleOutputScope : IDisposable
    {
        private ExampleOutputScope(string outputPath)
        {
            OutputPath = outputPath;
        }

        /// <summary>
        /// example 검증 출력 경로입니다.
        /// </summary>
        public string OutputPath { get; }

        /// <summary>
        /// 임시 출력 scope를 생성합니다.
        /// </summary>
        /// <returns>생성된 scope입니다.</returns>
        public static ExampleOutputScope Create()
        {
            var outputPath = Path.Combine(
                Path.GetTempPath(),
                "MilStd1553",
                "MockCliExample",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(outputPath);
            return new ExampleOutputScope(outputPath);
        }

        /// <summary>
        /// 임시 출력 디렉터리를 정리합니다.
        /// </summary>
        public void Dispose()
        {
            if (!Directory.Exists(OutputPath))
            {
                return;
            }

            try
            {
                Directory.Delete(OutputPath, true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
