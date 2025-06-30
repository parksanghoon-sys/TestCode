public enum NotificationType
{
    Email,
    SMS,
    Push
}

public enum Priority
{
    Low,
    Normal,
    High,
    Critical
}
public class NotificationRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public NotificationType Type { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Priority Priority { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    public DateTime ScheduledAt { get; set; } = DateTime.UtcNow;
}
public class NotificationResult
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime SentAt { get; set; }
    public string ExternalId { get; set; }
}

public interface INotificationStrategy
{
    Task<NotificationResult> SendAsync(NotificationRequest request);
}
public class EmailNotificationStrategy : INotificationStrategy
{
    public async Task<NotificationResult> SendAsync(NotificationRequest request)
    {
        try
        {
            await Task.Delay(Random.Shared.Next(100, 500));
            Console.WriteLine($"Email sent to {request.Recipient}: {request.Subject}");

            return new NotificationResult
            {
                IsSuccess = true,
                SentAt = DateTime.UtcNow,
                ExternalId = $"email_{Guid.NewGuid():N}"
            };

        }
        catch (Exception ex)
        {

            return new NotificationResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                SentAt = DateTime.UtcNow
            };
        }        
    }
}
public class SmsNotificationStrategy : INotificationStrategy
{
    public async Task<NotificationResult> SendAsync(NotificationRequest request)
    {
        try
        {
            await Task.Delay(Random.Shared.Next(50, 200));

            Console.WriteLine($"SMS sent to {request.Recipient}: {request.Message}");

            return new NotificationResult
            {
                IsSuccess = true,
                SentAt = DateTime.UtcNow,
                ExternalId = $"sms_{Guid.NewGuid():N}"
            };
        }
        catch (Exception ex)
        {
            return new NotificationResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                SentAt = DateTime.UtcNow
            };
        }
    }
}
