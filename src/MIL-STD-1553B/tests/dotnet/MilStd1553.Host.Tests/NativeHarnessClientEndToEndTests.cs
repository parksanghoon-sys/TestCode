using System.Collections.ObjectModel;
using System.Diagnostics;
using MilStd1553.Host.Models;
using MilStd1553.Interop.Exceptions;
using MilStd1553.Interop.NativeMethods;
using MilStd1553.Interop.Services;

namespace MilStd1553.Host.Tests;

/// <summary>
/// 실제 네이티브 런타임 로더 경계와 `NativeHarnessClient` end-to-end 동작을 검증합니다.
/// </summary>
[Collection("Native runtime loader")]
public sealed class NativeHarnessClientEndToEndTests
{
    /// <summary>
    /// 런타임 레이아웃에 배치한 실제 네이티브 DLL로 세션 lifecycle이 동작해야 합니다.
    /// </summary>
    [Fact]
    public async Task NativeHarnessClient_WithRuntimeLayoutNativeDll_CompletesSessionLifecycle()
    {
        using var runtimeRootScope = NativeRuntimeRootScope.Create();
        CopyBuiltArtifactToRuntimeLayout("NativeDll", runtimeRootScope.RuntimeRootPath);

        var client = new NativeHarnessClient();
        var sessionStart = await client.OpenSessionAsync(CreateScenario(), CancellationToken.None);

        Assert.StartsWith("session-", sessionStart.SessionHandle.Value, StringComparison.Ordinal);
        Assert.Equal(BusLine.B, sessionStart.InitialHealth.ActiveBus);

        await client.SwitchBusAsync(sessionStart.SessionHandle, BusLine.A, CancellationToken.None);

        var health = await client.GetHealthSnapshotAsync(sessionStart.SessionHandle, CancellationToken.None);
        Assert.Equal(BusLine.A, health.ActiveBus);

        var events = await client.PollTelemetryAsync(sessionStart.SessionHandle, CancellationToken.None);
        Assert.Single(events);
        Assert.Equal(TelemetryEventType.BusSwitch, events[0].Type);
        Assert.Equal(BusLine.A, events[0].ActiveBus);

        var emptyEvents = await client.PollTelemetryAsync(sessionStart.SessionHandle, CancellationToken.None);
        Assert.Empty(emptyEvents);

        await client.StopSessionAsync(sessionStart.SessionHandle, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<NativeInteropException>(() =>
            client.GetHealthSnapshotAsync(sessionStart.SessionHandle, CancellationToken.None));
        Assert.Equal(2, exception.StatusCode);
    }

    /// <summary>
    /// 런타임 DLL이 없으면 로더 경계가 명시적인 예외로 실패해야 합니다.
    /// </summary>
    [Fact]
    public async Task NativeHarnessClient_WhenRuntimeDllIsMissing_ThrowsNativeLibraryLoadException()
    {
        using var runtimeRootScope = NativeRuntimeRootScope.Create();
        var client = new NativeHarnessClient();

        var exception = await Assert.ThrowsAsync<NativeLibraryLoadException>(() =>
            client.OpenSessionAsync(CreateScenario(), CancellationToken.None));

        Assert.Equal(NativeLibraryFailureKind.LibraryNotFound, exception.FailureKind);
        Assert.Equal(
            NativeLibraryPathResolver.GetExpectedLibraryPath(runtimeRootScope.RuntimeRootPath),
            exception.LibraryPath);
    }

    /// <summary>
    /// 잘못된 네이티브 바이너리를 적재하면 로더 실패 예외로 변환해야 합니다.
    /// </summary>
    [Fact]
    public async Task NativeHarnessClient_WhenRuntimeDllIsInvalid_ThrowsNativeLibraryLoadException()
    {
        using var runtimeRootScope = NativeRuntimeRootScope.Create();
        var targetDllPath = NativeLibraryPathResolver.GetExpectedLibraryPath(runtimeRootScope.RuntimeRootPath);
        Directory.CreateDirectory(Path.GetDirectoryName(targetDllPath)!);
        File.WriteAllText(targetDllPath, "invalid-native-binary");

        var client = new NativeHarnessClient();

        var exception = await Assert.ThrowsAsync<NativeLibraryLoadException>(() =>
            client.OpenSessionAsync(CreateScenario(), CancellationToken.None));

        Assert.Equal(NativeLibraryFailureKind.InvalidBinary, exception.FailureKind);
        Assert.Equal(targetDllPath, exception.LibraryPath);
    }

    /// <summary>
    /// 필요한 엔트리 포인트가 없는 DLL은 버전 불일치 예외로 실패해야 합니다.
    /// </summary>
    [Fact]
    public async Task NativeHarnessClient_WhenRuntimeDllMissesRequiredEntryPoint_ThrowsNativeLibraryLoadException()
    {
        using var runtimeRootScope = NativeRuntimeRootScope.Create();
        CopyBuiltArtifactToRuntimeLayout("PlaceholderDll", runtimeRootScope.RuntimeRootPath);

        var client = new NativeHarnessClient();

        var exception = await Assert.ThrowsAsync<NativeLibraryLoadException>(() =>
            client.OpenSessionAsync(CreateScenario(), CancellationToken.None));

        Assert.Equal(NativeLibraryFailureKind.EntryPointMissing, exception.FailureKind);
        Assert.Contains("OpenSession", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 공통 smoke test 시나리오를 생성합니다.
    /// </summary>
    /// <returns>기본 smoke test 시나리오입니다.</returns>
    private static ScenarioDefinition CreateScenario()
    {
        return new ScenarioDefinition(
            "SmokeInterop",
            0,
            BusLine.B,
            new ReadOnlyCollection<ScenarioScheduleDefinition>(
            [
                new ScenarioScheduleDefinition("PollRt1", 20, 1, 2, TransferDirection.Receive),
            ]));
    }

    /// <summary>
    /// 지정한 네이티브 아티팩트를 런타임 레이아웃 경로로 복사합니다.
    /// </summary>
    /// <param name="artifactType">빌드할 네이티브 아티팩트 종류입니다.</param>
    /// <param name="runtimeRootPath">대상 런타임 루트 경로입니다.</param>
    private static void CopyBuiltArtifactToRuntimeLayout(string artifactType, string runtimeRootPath)
    {
        var repositoryRoot = FindRepositoryRoot();
        var buildScriptPath = Path.Combine(repositoryRoot.FullName, "tests", "native", "build_native_artifact.ps1");
        var nativeDllPath = Path.Combine(repositoryRoot.FullName, "tests", "native", "artifacts", "MilStd1553.Native.dll");
        var targetDllPath = NativeLibraryPathResolver.GetExpectedLibraryPath(runtimeRootPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{buildScriptPath}\" -ArtifactType {artifactType}",
            WorkingDirectory = repositoryRoot.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("네이티브 DLL 빌드 프로세스를 시작하지 못했습니다.");

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var standardOutput = process.StandardOutput.ReadToEnd();
            var standardError = process.StandardError.ReadToEnd();
            throw new InvalidOperationException(
                $"네이티브 DLL 빌드가 실패했습니다.{Environment.NewLine}{standardOutput}{Environment.NewLine}{standardError}");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(targetDllPath)!);
        File.Copy(nativeDllPath, targetDllPath, true);
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
    /// 테스트 전용 네이티브 런타임 루트 환경을 구성합니다.
    /// </summary>
    private sealed class NativeRuntimeRootScope : IDisposable
    {
        private readonly string? previousRuntimeRoot;

        private NativeRuntimeRootScope(string runtimeRootPath)
        {
            RuntimeRootPath = runtimeRootPath;
            previousRuntimeRoot = Environment.GetEnvironmentVariable("MILSTD1553_NATIVE_RUNTIME_ROOT");
            Environment.SetEnvironmentVariable("MILSTD1553_NATIVE_RUNTIME_ROOT", runtimeRootPath);
        }

        /// <summary>
        /// 테스트에 사용할 런타임 루트 경로입니다.
        /// </summary>
        public string RuntimeRootPath { get; }

        /// <summary>
        /// 새 테스트 런타임 루트 scope를 생성합니다.
        /// </summary>
        /// <returns>생성된 scope입니다.</returns>
        public static NativeRuntimeRootScope Create()
        {
            var runtimeRootPath = Path.Combine(
                Path.GetTempPath(),
                "MilStd1553",
                "NativeRuntimeTests",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(runtimeRootPath);
            return new NativeRuntimeRootScope(runtimeRootPath);
        }

        /// <summary>
        /// 환경 변수를 복원하고 임시 디렉터리를 정리합니다.
        /// </summary>
        public void Dispose()
        {
            Environment.SetEnvironmentVariable("MILSTD1553_NATIVE_RUNTIME_ROOT", previousRuntimeRoot);

            if (!Directory.Exists(RuntimeRootPath))
            {
                return;
            }

            try
            {
                Directory.Delete(RuntimeRootPath, true);
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
