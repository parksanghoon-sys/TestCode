using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chainResultPattern.Processors;

// 요청 처리기 - 체인 방식으로 요청 처리
internal class RequestProcessor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<object>> _pendingRequests = new();

    public RequestProcessor(IServiceProvider serviceProvider, ILogger logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    public async Task<bool> ProcessRequestsAsync<TRequset, TResponse>(TRequset requset, 
        Func<Result<TResponse>, IRequest<object>> nextRequestFactory,
        CancellationToken cancellationToken = default)
        where TRequset : IRequest<TResponse>
    {
        // 첫번쨰 요청의 핸들러 가져오기
        var handler = (IRequestHandler < TRequset, TResponse >) _serviceProvider.GetService(typeof(IRequestHandler<TRequset, TResponse>));

        if (handler == null)
        {
            _logger.Log($"오류: {typeof(TRequset).Name}에 대한 핸들러를 찾을 수 없습니다");
            return false;
        }
        _logger.Log($"요청 체인 시작: {requset.RequestName} (ID: {requset.RequestId})");
        var tcs = new TaskCompletionSource<object>();
        _pendingRequests.TryAdd(requset.RequestId, tcs);

        try
        {
            var result = await handler.HandleAsync(requset, cancellationToken);

            if(result.IsSuccess)
            {
                _logger.Log($"{requset.RequestName} 성공적으로 처리됨 (ID: {requset.RequestId})");
                _logger.Log($"결과: {result.Data}");
                tcs.TrySetResult(result.Data);
            }
            else
            {
                _logger.Log($"{requset.RequestName} 처리 실패 (ID: {requset.RequestId}): {result.ErrorMessage}");
                tcs.TrySetException(new InvalidOperationException(result.ErrorMessage));
                return false; // 첫 번째 요청 실패 시 체인 중단
            }
            // 성공 시 다음 요청 결정 및 처리
            if(result.IsSuccess && nextRequestFactory != null)
            {
                var nextRequst = nextRequestFactory(result);
                if(nextRequst is not null)
                    return await ProcessNextRequstAsync(nextRequst, cancellationToken);
            }
            return result.IsSuccess;
        }
        catch (OperationCanceledException)
        {
            _logger.Log($"{requset.RequestName} 취소됨 (ID: {requset.RequestId})");
            tcs.TrySetCanceled();
            return false;
        }
        catch (Exception ex)
        {
            _logger.Log($"{requset.RequestName} 처리 중 예외 발생 (ID: {requset.RequestId}): {ex.Message}");
            tcs.TrySetException(ex);
            return false;
        }
        finally
        {
            _pendingRequests.TryRemove(requset.RequestId, out _);
        }

    }
    private async Task<bool> ProcessNextRequstAsync(IRequest<object> request, CancellationToken cancellationToken = default)
    {
        // 이 메서드는 체인의 후속 요청 처리를 다룹니다
        // 실제 구현에서는 리플렉션이나 좀 더 정교한 방법을 사용하여
        // 제네릭 타입에 맞는 올바른 핸들러를 가져와야 합니다

        _logger.Log($"체인의 다음 요청 처리: {request.RequestName} (ID: {request.RequestId})");

        // 여기서 요청 유형에 따라 처리 계속
        // 실제 구현 시 동적으로 핸들러를 찾아 처리하도록 구현
        return true;
    }
    public void CancelRequest(Guid requestId)
    {
        if (_pendingRequests.TryGetValue(requestId, out var tcs))
        {
            _logger.Log($"요청 취소 중: {requestId}");
            tcs.TrySetCanceled();
        }
    }
}
