
// 스레드 안전한 콘솔 로거
public class ConsoleLogger : ILogger
{
    private readonly object _lock = new object();

    public void Log(string message)
    {
        lock (_lock)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");
        }
    }
}