namespace Notification_Pattern;

/// <summary>
/// 지정된 최대 재시도 횟수와 지연을 사용하여 알림 전송을 재시도하는 데코레이터입니다.
/// </summary>
public class RetryNotificationDecorator : INotificationStrategy
{
    private readonly INotificationStrategy _innerStrategy;
    private readonly int _maxRetries;
    private readonly TimeSpan _baseDelay;

    /// <summary>
    /// RetryNotificationDecorator의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="notificationStrategy">내부 알림 전략</param>
    /// <param name="maxRetries">최대 재시도 횟수</param>
    /// <param name="baseDelay">기본 지연 시간(옵션)</param>
    public RetryNotificationDecorator(INotificationStrategy notificationStrategy, int maxRetries, TimeSpan? baseDelay = null)
    {
        _innerStrategy = notificationStrategy;
        _maxRetries = maxRetries;
        _baseDelay = baseDelay ?? TimeSpan.FromSeconds(1);
    }

    /// <summary>
    /// 알림을 전송하고 실패 시 재시도합니다.
    /// </summary>
    /// <param name="request">알림 요청</param>
    /// <returns>알림 결과</returns>
    public async Task<NotificationResult> SendAsync(NotificationRequest request)
    {
        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                var result = await _innerStrategy.SendAsync(request);
                if (result.IsSuccess is true)
                {
                    return result;
                }
                if (attempt == _maxRetries)
                {
                    return new NotificationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Failed after {attempt} attempts: {result.ErrorMessage}",
                        SentAt = DateTime.UtcNow
                    };
                }
            }
            catch (Exception ex)
            {
                if (attempt == _maxRetries)
                {
                    return new NotificationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = ex.Message,
                        SentAt = DateTime.UtcNow
                    };
                }
            }
            // 지수 백오프(Exponential backoff)
            var delay = TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
            await Task.Delay(delay);
        }
        return new NotificationResult
        {
            IsSuccess = false,
            ErrorMessage = "Max retries exceeded",
            SentAt = DateTime.UtcNow
        };
    }
}
/// <summary>
/// 지정된 시간 창 내에서 최대 요청 수를 제한하는 데코레이터입니다.
/// </summary>
public class RateLimitingDecorator : INotificationStrategy
{
    private readonly INotificationStrategy _inner;
    private readonly SemaphoreSlim _semaphore;
    private readonly TimeSpan _timeWindow;
    private readonly Queue<DateTime> _requestTimes;

    /// <summary>
    /// RateLimitingDecorator의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="inner">내부 알림 전략</param>
    /// <param name="maxRequestsPerWindow">시간 창당 최대 요청 수</param>
    /// <param name="timeWindow">시간 창</param>
    public RateLimitingDecorator(INotificationStrategy inner, int maxRequestsPerWindow, TimeSpan timeWindow)
    {
        _inner = inner;
        _semaphore = new SemaphoreSlim(maxRequestsPerWindow, maxRequestsPerWindow);
        _timeWindow = timeWindow;
        _requestTimes = new Queue<DateTime>();
    }

    /// <summary>
    /// 알림을 전송하며, 시간 창 내 요청 수를 제한합니다.
    /// </summary>
    /// <param name="request">알림 요청</param>
    /// <returns>알림 결과</returns>
    public async Task<NotificationResult> SendAsync(NotificationRequest request)
    {
        await _semaphore.WaitAsync();

        try
        {
            // 시간 창을 벗어난 이전 요청 정리
            var cutoff = DateTime.UtcNow - _timeWindow;
            while (_requestTimes.Count > 0 && _requestTimes.Peek() < cutoff)
            {
                _requestTimes.Dequeue();
            }

            _requestTimes.Enqueue(DateTime.UtcNow);
            return await _inner.SendAsync(request);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}