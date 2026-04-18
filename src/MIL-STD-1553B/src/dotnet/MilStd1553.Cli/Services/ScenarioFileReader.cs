using MilStd1553.Cli.Contracts;

namespace MilStd1553.Cli.Services;

/// <summary>
/// 파일 시스템에서 시나리오 JSON을 읽는 기본 구현입니다.
/// </summary>
public sealed class ScenarioFileReader : IScenarioFileReader
{
    /// <summary>
    /// 지정한 파일 경로의 전체 본문을 읽습니다.
    /// </summary>
    /// <param name="path">읽을 파일 경로입니다.</param>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>파일 본문입니다.</returns>
    public Task<string> ReadAllTextAsync(
        string path,
        CancellationToken cancellationToken)
    {
        return File.ReadAllTextAsync(path, cancellationToken);
    }
}
