namespace MilStd1553.Cli.Contracts;

/// <summary>
/// CLI가 시나리오 파일 본문을 읽을 때 사용하는 계약입니다.
/// </summary>
public interface IScenarioFileReader
{
    /// <summary>
    /// 지정한 경로의 시나리오 파일 전체 본문을 읽습니다.
    /// </summary>
    /// <param name="path">읽을 시나리오 파일 경로입니다.</param>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>시나리오 JSON 본문입니다.</returns>
    Task<string> ReadAllTextAsync(
        string path,
        CancellationToken cancellationToken);
}
