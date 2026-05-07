namespace CleanProtocolSample.Logging;

/// <summary>
/// 콘솔 로그 구현.
/// </summary>
public sealed class ConsoleLogger : IAppLogger
{
    public void Info(string message)
    {
        Console.WriteLine($"[INFO ] {message}");
    }

    public void Warn(string message)
    {
        Console.WriteLine($"[WARN ] {message}");
    }

    public void Error(string message)
    {
        Console.WriteLine($"[ERROR] {message}");
    }
}