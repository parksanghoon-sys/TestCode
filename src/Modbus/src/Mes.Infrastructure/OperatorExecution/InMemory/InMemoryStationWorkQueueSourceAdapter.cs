using Mes.Application.Contracts.OperatorExecution;
using Mes.Application.OperatorExecution;
using Mes.Application.OperatorExecution.WorkQueue;

namespace Mes.Infrastructure.OperatorExecution.InMemory;

/// <summary>
/// 작업 큐 source 포트를 in-memory 기준 저장소로 구현합니다.
/// </summary>
public sealed class InMemoryStationWorkQueueSourceAdapter : IStationWorkQueueSourcePort
{
    private readonly InMemoryOperatorExecutionStore _store;

    /// <summary>
    /// in-memory 작업 큐 source 어댑터를 초기화합니다.
    /// </summary>
    /// <param name="store">기준 저장소입니다.</param>
    public InMemoryStationWorkQueueSourceAdapter(InMemoryOperatorExecutionStore store)
    {
        _store = store;
    }

    /// <summary>
    /// 현재 저장소 상태에서 작업 큐 source snapshot을 로드합니다.
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
