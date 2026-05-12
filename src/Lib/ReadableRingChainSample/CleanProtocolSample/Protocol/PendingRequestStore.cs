using System.Collections.Concurrent;

namespace CleanProtocolSample.Protocol;
/// <summary>
/// Request/Response 매칭 저장소
/// </summary>
internal sealed class PendingRequestStore
{
    /// <summary>
    /// 결정되지 않은 처리 요청 ex)대기중, 처리중, 보류중
    /// </summary>
    private readonly ConcurrentDictionary<int, TaskCompletionSource<ProtocolMessage>> _pending = new();
    /// <summary>
    /// pending 요청 등록
    /// </summary>
    public Task<ProtocolMessage> Register(int correlationId)
    {
        var source = new TaskCompletionSource<ProtocolMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (_pending.TryAdd(correlationId, source) == false)
        {
            throw new InvalidOperationException(
                $"Duplicate correlationId: {correlationId}");
        }
        return source.Task;
    }
    /// <summary>
    /// 응답 도착 처리
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public bool TryComplete(ProtocolMessage message)
    {
        if(_pending.TryRemove(message.CorrelationId, out var source) == false)
        {
            return false;
        }
        return source.TrySetResult(message);
    }
}
