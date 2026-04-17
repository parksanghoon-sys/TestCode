namespace Mes.Infrastructure.OperatorExecution.Sqlite;

/// <summary>
/// SQLite 기반 operator-execution 저장소 파일 경로 옵션입니다.
/// </summary>
public sealed class SqliteOperatorExecutionStoreOptions
{
    /// <summary>
    /// SQLite 데이터베이스 파일의 절대 또는 상대 경로입니다.
    /// </summary>
    public string DatabaseFilePath { get; init; } = string.Empty;
}
