using System.Diagnostics;
using MilStd1553.Interop.NativeMethods;

namespace MilStd1553.Host.Tests;

/// <summary>
/// CLI 프로젝트 build/publish 출력이 실행 파일과 런타임 DLL 구성을 함께 포함하는지 검증합니다.
/// </summary>
[Collection("Native runtime loader")]
public sealed class CliPackagingTests
{
    /// <summary>
    /// `dotnet build`가 CLI 실행 파일과 managed/native DLL을 함께 출력해야 합니다.
    /// </summary>
    [Fact]
    public void DotnetBuild_OnCliProject_EmitsExecutableAndRequiredDlls()
    {
        using var outputScope = BuildOutputScope.Create("cli-build");

        RunDotnetCommand(
            $"build \"{GetCliProjectPath()}\" -c Debug -o \"{outputScope.OutputPath}\"");

        AssertRequiredOutputFiles(outputScope.OutputPath);
    }

    /// <summary>
    /// `dotnet publish`가 CLI 기준 동일한 runtime layout을 유지해야 합니다.
    /// </summary>
    [Fact]
    public void DotnetPublish_OnCliProject_EmitsExecutableAndRequiredDlls()
    {
        using var outputScope = BuildOutputScope.Create("cli-publish");

        RunDotnetCommand(
            $"publish \"{GetCliProjectPath()}\" -c Debug -o \"{outputScope.OutputPath}\"");

        AssertRequiredOutputFiles(outputScope.OutputPath);
    }

    /// <summary>
    /// 출력 루트에 필요한 실행 파일과 DLL 구성이 모두 존재하는지 확인합니다.
    /// </summary>
    /// <param name="outputPath">검사할 출력 경로입니다.</param>
    private static void AssertRequiredOutputFiles(string outputPath)
    {
        Assert.True(
            File.Exists(Path.Combine(outputPath, "MilStd1553.Cli.exe")) || File.Exists(Path.Combine(outputPath, "MilStd1553.Cli.dll")),
            "출력 루트에 MilStd1553.Cli 실행 파일이 없습니다.");
        Assert.True(
            File.Exists(Path.Combine(outputPath, "MilStd1553.Interop.dll")),
            "출력 루트에 MilStd1553.Interop.dll이 없습니다.");
        Assert.True(
            File.Exists(Path.Combine(outputPath, "MilStd1553.Host.dll")),
            "출력 루트에 MilStd1553.Host.dll이 없습니다.");
        Assert.True(
            File.Exists(NativeLibraryPathResolver.GetExpectedLibraryPath(outputPath)),
            "출력 루트에 runtime layout 아래 MilStd1553.Native.dll이 없습니다.");
    }

    /// <summary>
    /// `MilStd1553.Cli.csproj` 절대 경로를 반환합니다.
    /// </summary>
    /// <returns>CLI 프로젝트 경로입니다.</returns>
    private static string GetCliProjectPath()
    {
        return Path.Combine(
            FindRepositoryRoot().FullName,
            "src",
            "dotnet",
            "MilStd1553.Cli",
            "MilStd1553.Cli.csproj");
    }

    /// <summary>
    /// `dotnet` 명령을 실행하고 실패 시 표준 출력과 오류를 함께 보여줍니다.
    /// </summary>
    /// <param name="arguments">실행 인자입니다.</param>
    private static void RunDotnetCommand(string arguments)
    {
        var repositoryRoot = FindRepositoryRoot();
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = repositoryRoot.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("dotnet 프로세스를 시작하지 못했습니다.");

        process.WaitForExit();

        if (process.ExitCode == 0)
        {
            return;
        }

        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        throw new InvalidOperationException(
            $"dotnet 명령이 실패했습니다.{Environment.NewLine}{standardOutput}{Environment.NewLine}{standardError}");
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
    /// packaging 테스트용 임시 출력 디렉터리를 관리합니다.
    /// </summary>
    private sealed class BuildOutputScope : IDisposable
    {
        private BuildOutputScope(string outputPath)
        {
            OutputPath = outputPath;
        }

        /// <summary>
        /// 테스트 출력 경로입니다.
        /// </summary>
        public string OutputPath { get; }

        /// <summary>
        /// 새 packaging 출력 scope를 생성합니다.
        /// </summary>
        /// <param name="name">출력 종류 이름입니다.</param>
        /// <returns>생성된 scope입니다.</returns>
        public static BuildOutputScope Create(string name)
        {
            var outputPath = Path.Combine(
                Path.GetTempPath(),
                "MilStd1553",
                "PackagingTests",
                name,
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(outputPath);
            return new BuildOutputScope(outputPath);
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
