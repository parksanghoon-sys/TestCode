using chainResultPattern.Shared;

namespace chainResultPattern.Requests.Handlers;

public class SecondRequestHandler : IRequestHandler<SecondRequest, SecondResponse>
{
    private readonly ILogger _logger;
    private readonly SharedResourceManager _resourceManager;

    public SecondRequestHandler(ILogger logger)
    {
        _logger = logger;
        _resourceManager = SharedResourceManager.Instance;
    }

    public async Task<Result<SecondResponse>> HandleAsync(SecondRequest request, CancellationToken cancellationToken = default)
    {
        string resourceKey = "SharedDatabase";
        string calculationResourceKey = "CalculationEngine";

        _logger.Log($"[스레드 {Thread.CurrentThread.ManagedThreadId}] {request.RequestName} 처리 시작 (ID: {request.RequestId}), 파라미터: {request.Parameter}");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 두 개의 리소스를 순차적으로 획득 (교착상태 방지를 위해 항상 같은 순서로 획득)
            using (await _resourceManager.AccessResourceForWritingAsync(calculationResourceKey, cancellationToken))
            using (await _resourceManager.AccessResourceForWritingAsync(resourceKey, cancellationToken))
            {
                _logger.Log($"[스레드 {Thread.CurrentThread.ManagedThreadId}] 리소스 '{calculationResourceKey}' 및 '{resourceKey}' 독점 접근 획득");

                // 처리 시간 시뮬레이션
                await Task.Delay(500, cancellationToken);

                // 공유 데이터 처리 예시
                int lastCalculatedValue = 0;
                if (_resourceManager.TryGetSharedData<int>("LastCalculatedValue", out var lastValue))
                {
                    lastCalculatedValue = lastValue;
                    _logger.Log($"[스레드 {Thread.CurrentThread.ManagedThreadId}] 마지막 계산 값: {lastCalculatedValue}");
                }

                // 샘플 로직
                if (request.Parameter < 0)
                {
                    _logger.Log($"[스레드 {Thread.CurrentThread.ManagedThreadId}] {request.RequestName} 실패: 파라미터가 음수임");
                    return Result<SecondResponse>.Failure("파라미터는 음수가 아니어야 합니다");
                }

                // 새 계산 값 저장
                int newCalculatedValue = request.Parameter * 2;
                _resourceManager.SetSharedData("LastCalculatedValue", newCalculatedValue);

                _logger.Log($"[스레드 {Thread.CurrentThread.ManagedThreadId}] {request.RequestName} 성공적으로 처리됨");
                return Result<SecondResponse>.Success(new SecondResponse { Result = newCalculatedValue });
            }
        }
        catch (OperationCanceledException)
        {
            _logger.Log($"[스레드 {Thread.CurrentThread.ManagedThreadId}] {request.RequestName} 취소됨");
            return Result<SecondResponse>.Failure("요청이 취소되었습니다");
        }
        catch (Exception ex)
        {
            _logger.Log($"[스레드 {Thread.CurrentThread.ManagedThreadId}] {request.RequestName} 오류 발생: {ex.Message}");
            return Result<SecondResponse>.Failure($"요청 처리 중 오류 발생: {ex.Message}");
        }
    }
}

