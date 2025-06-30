using Serilog;

namespace Notification_Pattern;

public class NotificationFacade
{
    private readonly NotificationManager _manager;
    private readonly IUserPreferenceService _preferences;
    private readonly ITemplateService _templates;
    private readonly ILogger _logger;

    public NotificationFacade(ILogger logger)
    {
        _manager = NotificationManager.Instance;
        _preferences = new UserPreferenceService();
        _templates = new TemplateService();
        _logger = logger;

        //Register observers
        _manager.Subscribe(new AuditObserver(_logger));
        _manager.Subscribe(new MetricsObserver());

        //Configure decorated strategies
        SetupDecoratedStrategies();
       
    }

    public async Task<List<NotificationResult>> SendBatchNotificationAsync(List<string> userIds, NotificationType type, 
        string templateName, Dictionary<string, object> templateData)
    {
        var tasks = userIds.Select(userId =>
              SendNotificationAsync(userId, type, templateName, templateData));

        return (await Task.WhenAll(tasks)).ToList();
    }

    public async Task<NotificationResult> SendNotificationAsync(string userId, NotificationType type, 
        string templateName, Dictionary<string, object> templateData)
    {
        var preferences = await _preferences.GetUserPreferencesAsync(userId);
        if (preferences.IsChannelEnabled(type) is false)
        {
            return new NotificationResult
            {
                IsSuccess = false,
                ErrorMessage = "User has disabled this notification channel",
                SentAt = DateTime.UtcNow
            };
        }

        var template = await _templates.GetTemplateAsync(templateName, type);
        var processedContent = template.Process(templateData);
        // Create request
        var request = new NotificationRequest
        {
            Type = type,
            Recipient = preferences.GetChannelAddress(type),
            Subject = processedContent.Subject,
            Message = processedContent.Body,
            Priority = Priority.Normal
        };

        return await _manager.SendNotificationAsync(request);
    }

    private void SetupDecoratedStrategies()
    {
        // Email with retry and rate limiting
        var emailStrategy = new EmailNotificationStrategy();
        var decoratedEmail = new RateLimitingDecorator(
            new RetryNotificationDecorator(emailStrategy, 3),
            maxRequestsPerWindow: 10,
            timeWindow: TimeSpan.FromMinutes(1)
        );
        _manager.RegisterStrategy(NotificationType.Email, decoratedEmail);
        //SMS with retry only
        var smsStrategy = new SmsNotificationStrategy();
        var decoratedSms = new RetryNotificationDecorator(smsStrategy, 2);
        _manager.RegisterStrategy(NotificationType.SMS, decoratedSms);

    }
}

