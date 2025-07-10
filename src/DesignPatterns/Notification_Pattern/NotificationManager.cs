using Notification_Pattern;

public sealed class NotificationManager
{
    private static readonly Lazy<NotificationManager> _instance = new Lazy<NotificationManager>(() => new NotificationManager());

    private readonly Dictionary<NotificationType, INotificationStrategy> _strategies;
    private readonly List<INotificationObserver> _observers;
    private readonly object _lock = new object();
    public static NotificationManager Instance => _instance.Value;
    public NotificationManager()
    {
        _strategies = new();
        _observers = new List<INotificationObserver>();
        InitializeStrategies();
    }
    private void InitializeStrategies()
    {
        RegisterStrategy(NotificationType.Email, new EmailNotificationStrategy());
        RegisterStrategy(NotificationType.SMS, new SmsNotificationStrategy());        
    }

    public void RegisterStrategy(NotificationType type, INotificationStrategy strategy)
    {
        lock (_lock)
        {
            _strategies[type] = strategy;
        }
    }
    public async Task<NotificationResult> SendNotificationAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); // 취소 요청이 있다면 즉시 예외 발생

        if ( _strategies.TryGetValue(request.Type, out var strategy))
        {
            var result = await strategy.SendAsync(request);
            NotifyObservers(request, result);
            return result;
        }                
        throw new NotSupportedException($"Notification type {request.Type} is not supported.");        
    }
    public void Subscribe(INotificationObserver observer)
    {
        lock (_lock)
        {
            _observers.Add(observer);
        }
    }
    private void NotifyObservers(NotificationRequest request, NotificationResult result)
    {
        foreach(var observer in _observers)
        {
            observer.OnNotificationProcessed(request, result);
        }
    }
}
