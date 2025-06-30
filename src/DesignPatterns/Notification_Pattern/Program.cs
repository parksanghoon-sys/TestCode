using Notification_Pattern;
using Serilog;
using System.Net.NetworkInformation;

internal class Program
{
    public static async Task Main(string[] args)
    {
        // Serilog 구성 (파일 로그 포함)
        Log.Logger = new LoggerConfiguration()            
            .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
            .Enrich.FromLogContext()
            .CreateLogger();
      
        var facade = new NotificationFacade(Log.Logger);

        //Send single notification
        var result = await facade.SendNotificationAsync(
            userId: "user1",
            type: NotificationType.Email,
            templateName: "welcome",
            templateData: new Dictionary<string, object>
            {
                { "name", "John Doe" },
                { "product", "Enterprise Suite" }
            }
        );

        Console.WriteLine($"Notification sent: {result.IsSuccess}");

        //Send batch notifications
        var userIds = new List<string> { "user1", "user2" };
        var batchResults = await facade.SendBatchNotificationAsync(
            userIds,
            NotificationType.SMS,
            "promotion",
            new Dictionary<string, object> { { "discount", "20%" } }
        );

        Console.WriteLine($"Batch notifications sent: {batchResults.Count(r => r.IsSuccess)}/{batchResults.Count}");
    }
}
