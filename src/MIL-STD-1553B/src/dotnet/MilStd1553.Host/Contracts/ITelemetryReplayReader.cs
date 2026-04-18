using System.Collections.ObjectModel;
using MilStd1553.Host.Models;

namespace MilStd1553.Host.Contracts;

/// <summary>
/// BM JSONL 로그를 telemetry DTO로 다시 읽는 계약입니다.
/// </summary>
public interface ITelemetryReplayReader
{
    /// <summary>
    /// 지정한 JSONL 파일에서 telemetry event를 읽습니다.
    /// </summary>
    /// <param name="filePath">읽을 JSONL 파일 경로입니다.</param>
    /// <returns>복원된 telemetry event 목록입니다.</returns>
    ReadOnlyCollection<TelemetryEventRecord> ReadFromFile(string filePath);
}
