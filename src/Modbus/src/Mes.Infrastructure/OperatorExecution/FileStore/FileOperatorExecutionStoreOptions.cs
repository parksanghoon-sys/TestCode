namespace Mes.Infrastructure.OperatorExecution.FileStore;

/// <summary>
/// 파일 기반 operator-execution 저장소 경로 옵션입니다.
/// </summary>
public sealed class FileOperatorExecutionStoreOptions
{
    /// <summary>
    /// 저장소 JSON 파일의 절대 경로입니다.
    /// </summary>
    public string StoreFilePath { get; init; } = string.Empty;
}
