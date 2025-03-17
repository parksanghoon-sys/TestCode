using chainResultPattern.Shared;

namespace chainResultPattern.Requests.Handlers;

public class ThirdRequestHandler : IRequestHandler<ThirdRequest, ThirdResponse>
{
    private readonly ILogger _logger;
    private readonly SharedResourceManager _resourceManager;

    public ThirdRequestHandler(ILogger logger)
    {
        _logger = logger;
        _resourceManager = SharedResourceManager.Instance;
    }

    public async Task<Result<ThirdResponse>> HandleAsync(ThirdRequest request, CancellationToken cancellationToken = default)
    {
        string resourceKey = "AuditSystem";

        _logger.Log($"[스레드 {Thread.CurrentThread.ManagedThreadId}] {request.RequestName} 처리 시작 (ID: {request.RequestId}), 파라미터: {request.Parameter}");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 읽기 전용 접근으로 충분한 경우 (분석 또는 감사 등)
            if (await _resourceManager.TryAccessResourceForReadingAsync(resourceKey, cancellationToken))
            {
                _logger.Log($"[스레드 {Thread.CurrentThread.ManagedThreadId}] 리소스 '{resourceKey}' 읽기 접근 획득");

                // 처리 시간 시뮬레이션
                await Task.Delay(200, cancellationToken);

                // 읽기 작업 (작은 작업은 락 없이 진행 가능)
            }

            // 쓰기 작업이 필요한 경우 독점 락 획득
            using (await _resourceManager.AccessResourceForWritingAsync(resourceKey, cancellationToken))
            {
                _logger.Log($"[스레드 {Thread.CurrentThread.ManagedThreadId}] 리소스 '{resourceKey}' 독점 접근 획득");

                // 처리 시간 시뮬레이션
                await Task.Delay(300, cancellationToken);

                // 샘플 로직
                if (request.Parameter == Guid.Empty)
                {
                    _logger.Log($"[스레드 {Thread.CurrentThread.ManagedThreadId}] {request.RequestName} 실패: 파라미터가 빈 GUID임");
                    return Result<ThirdResponse>.Failure("파라미터는 빈 GUID가 아니어야 합니다");
                }

                // 감사 기록 추가 예시
                List<string> auditLog;
                if (!_resourceManager.TryGetSharedData<List<string>>("AuditLog", out auditLog))
                {
                    auditLog = new List<string>();
                }

                auditLog.Add($"{DateTime.Now}: 처리됨 - 요청 ID {request.RequestId}");
                _resourceManager.SetSharedData("AuditLog", auditLog);

                _logger.Log($"[스레드 {Thread.CurrentThread.ManagedThreadId}] {request.RequestName} 성공적으로 처리됨");
                return Result<ThirdResponse>.Success(new ThirdResponse { Result = true });
            }
        }
        catch (OperationCanceledException)
        {
            _logger.Log($"[스레드 {Thread.CurrentThread.ManagedThreadId}] {request.RequestName} 취소됨");
            return Result<ThirdResponse>.Failure("요청이 취소되었습니다");
        }
        catch (Exception ex)
        {
            _logger.Log($"[스레드 {Thread.CurrentThread.ManagedThreadId}] {request.RequestName} 오류 발생: {ex.Message}");
            return Result<ThirdResponse>.Failure($"요청 처리 중 오류 발생: {ex.Message}");
        }
    }
}


