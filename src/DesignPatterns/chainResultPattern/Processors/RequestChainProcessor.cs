using System.Collections.Concurrent;
using System.Xml;

namespace chainResultPattern.Processors;

public class RequestChainProcessor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _semaphore;
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _requestCancellations = new ConcurrentDictionary<Guid, CancellationTokenSource>();
    public RequestChainProcessor(IServiceProvider serviceProvider, ILogger logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount); // 병렬 처리 제한
    }
    public async Task<bool> ExecuteRequestChainAsync(List<object> requestChain, CancellationToken cancellationToken = default)
    {
        if (requestChain == null || requestChain.Count == 0)
        {
            _logger.Log("요청 체인이 비어 있습니다,");
            return false;
        }
        // 이 요청 체인을 위한 고유 ID 생성
        var chainId = Guid.NewGuid();
        var cahinCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _requestCancellations[chainId] = cahinCts;

        try
        {
            for(int i = 0; i < requestChain.Count; i++)
            {
                // 동시 처리 갯수를 제한하기 위해 세마포어 대기
                await _semaphore.WaitAsync(cahinCts.Token);
                try
                {
                    var request = requestChain[i];
                    var requestType = request.GetType();

                    // IRequest<TResponse> 에서 RequsetId 속성 가져오기
                    var requestIdProperty = requestType.GetProperty("RequestId");
                    var requestId = (Guid)requestIdProperty.GetValue(request);

                    var requestNameProperty = requestType.GetProperty("RequestName");
                    var requestName = (string)requestNameProperty.GetValue(request);

                    _logger.Log($"요청 {i + 1}/{requestChain.Count} 처리 중: {requestName} (ID: {requestId}, 체인 ID: {chainId})");

                    // 응답 타입 동적으로 가져오기
                    var responseType = requestType.GetInterfaces()
                        .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))
                        .GetGenericArguments()[0];

                    // 핸들러 타입 동적 가져오기
                    var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);

                    // 핸들러 인스턴스 가져오기
                    var hadler = _serviceProvider.GetService(handlerType);
                    if(hadler is null)
                    {
                        _logger.Log($"오류: 요청 타입에 대한 핸들러가 등록되지 않았습니다: {requestType.Name}");
                        return false;
                    }
                    // HandleAsync 메서드 실행
                    var handleMethod = handlerType.GetMethod("HandleAsync");
                    var task = (Task)handleMethod.Invoke(hadler, new[] {request, cahinCts.Token});
                    await task;
                    // 태스크에서 Result 속성 가져오기
                    var resultProperty = task.GetType().GetProperty("Result");
                    var result = resultProperty.GetValue(task);
                    // Result에서 IsSuccess 속성 가져오기
                    var isSuccessProperty = result.GetType().GetProperty("IsSuccess");
                    var isSuccess = (bool)isSuccessProperty.GetValue(result);
                    if (!isSuccess)
                    {
                        var errorProperty = result.GetType().GetProperty("ErrorMessage");
                        var errorMessage = (string)errorProperty.GetValue(result);
                        _logger.Log($"요청 체인 {chainId}이 단계 {i + 1}에서 실패: {errorMessage}");
                        return false;
                    }

                    _logger.Log($"요청 {i + 1} 성공적으로 처리됨 (체인 ID: {chainId})");

                }
                finally
                {
                    _semaphore.Release();

            }
            }
            _logger.Log($"요청 체인 {chainId}이 성공적으로 완료되었습니다.");
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.Log($"요청 체인 {chainId}이 취소되었습니다.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.Log($"요청 체인 {chainId} 처리 중 예외 발생: {ex.Message}");
            return false;
        }
        finally
        {
            // 정리 작업
            _requestCancellations.TryRemove(chainId, out _);
            cahinCts.Dispose();
        }
    }
    public void CancelRequestChain(Guid chainId)
    {
        if (_requestCancellations.TryGetValue(chainId, out var cts))
        {
            _logger.Log($"요청 체인 취소 중: {chainId}");
            cts.Cancel();
        }
    }

    public async Task<bool> ExecuteRequestsInParallelAsync(List<object> requests, int maxConcurrency = 0, CancellationToken cancellationToken = default)
    {
        if (requests == null || requests.Count == 0)
        {
            _logger.Log("요청 목록이 비어 있습니다.");
            return false;
        }

        // 최대 동시 실행 수 (기본값: 프로세서 수)
        if (maxConcurrency <= 0)
        {
            maxConcurrency = Environment.ProcessorCount;
        }

        var batchId = Guid.NewGuid();
        _logger.Log($"병렬 요청 배치 {batchId} 시작, 요청 수: {requests.Count}, 최대 동시 실행: {maxConcurrency}");

        var semaphore = new SemaphoreSlim(maxConcurrency);
        var tasks = new List<Task<bool>>();
        var batchCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        foreach (var request in requests)
        {
            await semaphore.WaitAsync(cancellationToken);

            tasks.Add(Task.Run(async () => {
                try
                {
                    var requestType = request.GetType();

                    // 요청 정보 가져오기
                    var requestIdProperty = requestType.GetProperty("RequestId");
                    var requestId = (Guid)requestIdProperty.GetValue(request);

                    var requestNameProperty = requestType.GetProperty("RequestName");
                    var requestName = (string)requestNameProperty.GetValue(request);

                    _logger.Log($"[병렬 처리] 요청 처리 시작: {requestName} (ID: {requestId}, 배치 ID: {batchId})");

                    // 응답 및 핸들러 타입 가져오기
                    var responseType = requestType.GetInterfaces()
                        .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))
                        .GetGenericArguments()[0];

                    var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
                    var handler = _serviceProvider.GetService(handlerType);

                    if (handler == null)
                    {
                        _logger.Log($"[병렬 처리] 오류: 요청 타입에 대한 핸들러가 등록되지 않았습니다: {requestType.Name}");
                        return false;
                    }

                    // 요청 처리
                    var handleMethod = handlerType.GetMethod("HandleAsync");
                    var task = (Task)handleMethod.Invoke(handler, new[] { request, batchCts.Token });
                    await task;

                    // 결과 확인
                    var resultProperty = task.GetType().GetProperty("Result");
                    var result = resultProperty.GetValue(task);

                    var isSuccessProperty = result.GetType().GetProperty("IsSuccess");
                    var isSuccess = (bool)isSuccessProperty.GetValue(result);

                    if (!isSuccess)
                    {
                        var errorProperty = result.GetType().GetProperty("ErrorMessage");
                        var errorMessage = (string)errorProperty.GetValue(result);
                        _logger.Log($"[병렬 처리] 요청 실패: {requestName} (ID: {requestId}): {errorMessage}");
                        return false;
                    }

                    _logger.Log($"[병렬 처리] 요청 성공: {requestName} (ID: {requestId})");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.Log($"[병렬 처리] 요청 처리 중 예외 발생: {ex.Message}");
                    return false;
                }
                finally
                {
                    semaphore.Release();
                }
            }, batchCts.Token));
        }

        try
        {
            // 모든 태스크 완료 대기
            var results = await Task.WhenAll(tasks);

            // 전체 성공 여부 확인
            bool allSucceeded = results.All(r => r);

            _logger.Log($"병렬 요청 배치 {batchId} 완료. 모든 요청 성공: {allSucceeded}");
            return allSucceeded;
        }
        catch (OperationCanceledException)
        {
            _logger.Log($"병렬 요청 배치 {batchId}가 취소되었습니다.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.Log($"병렬 요청 배치 {batchId} 처리 중 예외 발생: {ex.Message}");
            return false;
        }
        finally
        {
            batchCts.Dispose();
            semaphore.Dispose();
        }
    }
}
