using chainResultPattern.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chainResultPattern.Requests.Handlers;

// 샘플 요청 핸들러
public class FirstRequestHandler : IRequestHandler<FirstRequest, FirstResponse>
{
    private readonly ILogger _logger;
    private readonly SharedResourceManager _resourceManager;

    public FirstRequestHandler(ILogger logger)
    {
        _logger = logger;
        _resourceManager = SharedResourceManager.Instance;
    }

    public async Task<Result<FirstResponse>> HandleAsync(FirstRequest request, CancellationToken cancellationToken = default)
    {
        // 공유 리소스 키 정의 (실제 상황에 맞게 조정)
        string resourceKey = "SharedDatabase";

        _logger.Log($"[스레드 {Thread.CurrentThread.ManagedThreadId}] {request.RequestName} 처리 시작 (ID: {request.RequestId}), 파라미터: {request.Parameter}");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 읽기 전용 접근 확인 (필요한 경우)
            bool canReadResource = await _resourceManager.TryAccessResourceForReadingAsync(resourceKey, cancellationToken);
            _logger.Log($"[스레드 {Thread.CurrentThread.ManagedThreadId}] 리소스 '{resourceKey}' 읽기 가능 여부: {canReadResource}");

            // 독점적 접근이 필요한 경우 (쓰기 작업) - using 블록으로 자동 해제
            using (await _resourceManager.AccessResourceForWritingAsync(resourceKey, cancellationToken))
            {
                _logger.Log($"[스레드 {Thread.CurrentThread.ManagedThreadId}] 리소스 '{resourceKey}' 독점 접근 획득");

                // 처리 시간 시뮬레이션
                await Task.Delay(500, cancellationToken);

                // 공유 데이터 읽기 예시
                if (_resourceManager.TryGetSharedData<string>("LastProcessedRequest", out var lastRequest))
                {
                    _logger.Log($"[스레드 {Thread.CurrentThread.ManagedThreadId}] 마지막 처리된 요청: {lastRequest}");
                }

                // 공유 데이터 쓰기 예시
                _resourceManager.SetSharedData("LastProcessedRequest", request.RequestId.ToString());

                // 샘플 로직 - API 호출, 데이터베이스 접근 등이 될 수 있음
                if (string.IsNullOrEmpty(request.Parameter))
                {
                    _logger.Log($"[스레드 {Thread.CurrentThread.ManagedThreadId}] {request.RequestName} 실패: 파라미터가 비어 있음");
                    return Result<FirstResponse>.Failure("파라미터는 필수 입력값입니다");
                }

                _logger.Log($"[스레드 {Thread.CurrentThread.ManagedThreadId}] {request.RequestName} 성공적으로 처리됨");
                return Result<FirstResponse>.Success(new FirstResponse { Result = $"처리됨: {request.Parameter}" });
            }
        }
        catch (OperationCanceledException)
        {
            _logger.Log($"[스레드 {Thread.CurrentThread.ManagedThreadId}] {request.RequestName} 취소됨");
            return Result<FirstResponse>.Failure("요청이 취소되었습니다");
        }
        catch (Exception ex)
        {
            _logger.Log($"[스레드 {Thread.CurrentThread.ManagedThreadId}] {request.RequestName} 오류 발생: {ex.Message}");
            return Result<FirstResponse>.Failure($"요청 처리 중 오류 발생: {ex.Message}");
        }
    }
}
