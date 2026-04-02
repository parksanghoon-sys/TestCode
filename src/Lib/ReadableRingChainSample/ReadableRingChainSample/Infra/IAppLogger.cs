namespace ReadableRingChainSample.Infra;

public interface IAppLogger
{
    void Info(string message);
    void Warning(string message);
    void Error(string message);
}
public sealed class ConsoleLogger : IAppLogger
{
    public void Info(string message)
    {
        Console.WriteLine($"[INFO] {message}");
    }
    public void Warning(string message)
    {
        Console.WriteLine($"[WARNING] {message}");
    }
    public void Error(string message)
    {
        Console.WriteLine($"[ERROR] {message}");
    }
}
