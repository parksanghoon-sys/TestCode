using Serilog;
using System.Text.Json;

namespace Notification_Pattern;

public interface INotificationObserver
{
    void OnNotificationProcessed(NotificationRequest request, NotificationResult result);
}
public class AuditObserver : INotificationObserver
{
    private readonly ILogger _logger;

    public AuditObserver(ILogger logger)
    {
        _logger = logger;
    }

    public void OnNotificationProcessed(NotificationRequest request, NotificationResult result)
    {
        var auditEntry = new
        {
            RequestId = request.Id,
            Type = request.Type.ToString(),
            Recipient = request.Recipient,
            Priority = request.Priority.ToString(),
            Success = result.IsSuccess,
            SentAt = result.SentAt,
            ErrorMessage = result.ErrorMessage
        };
        _logger.Information($"Audit: {JsonSerializer.Serialize(auditEntry)}");
    }
}
public class MetricsObserver : INotificationObserver
{
    private static readonly Dictionary<NotificationType, int> _successCounts =
        new Dictionary<NotificationType, int>();
    private static readonly Dictionary<NotificationType, int> _failureCounts =
        new Dictionary<NotificationType, int>();

    public void OnNotificationProcessed(NotificationRequest request, NotificationResult result)
    {
        var counters = result.IsSuccess ? _successCounts : _failureCounts;

        if (counters.ContainsKey(request.Type))
            counters[request.Type]++;
        else
            counters[request.Type] = 1;

        Console.WriteLine($"Metrics - {request.Type}: Success={_successCounts.GetValueOrDefault(request.Type, 0)}, " +
                         $"Failures={_failureCounts.GetValueOrDefault(request.Type, 0)}");
    }
}


