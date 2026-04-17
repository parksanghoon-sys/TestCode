using Mes.Application.Contracts.OperatorExecution;
using Mes.Application.OperatorExecution;
using Mes.Application.OperatorExecution.WorkQueue;

namespace Mes.Infrastructure.OperatorExecution.Sqlite;

/// <summary>
/// 작업 큐 source port를 SQLite 기반 durable 저장소로 구현합니다.
/// </summary>
public sealed class SqliteStationWorkQueueSourceAdapter : IStationWorkQueueSourcePort
{
    private readonly SqliteOperatorExecutionStore _store;

    /// <summary>
    /// SQLite 기반 작업 큐 source adapter를 초기화합니다.
    /// </summary>
    /// <param name="store">SQLite 기반 저장소입니다.</param>
    public SqliteStationWorkQueueSourceAdapter(SqliteOperatorExecutionStore store)
    {
        _store = store;
    }

    /// <summary>
    /// 현재 저장소 상태에서 station work queue source snapshot을 로드합니다.
    /// </summary>
    /// <param name="request">작업 큐 query 계약입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>MES-side source snapshot입니다.</returns>
    public Task<StationWorkQueueSource> LoadSourceAsync(
        GetStationWorkQueueRequestContract request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromResult(_store.BuildWorkQueueSource());
    }
}
