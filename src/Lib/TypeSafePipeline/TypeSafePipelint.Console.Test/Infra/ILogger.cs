using TypeSafePipelint.Console.Test.Infra;

namespace TypeSafePipelint.Console.Test.Infra
{
    // 로거 인터페이스
    public interface ILogger
    {
        void Log(string message);
    }
}
// 콘솔 로거 구현
public class ConsoleLogger : ILogger
{
    public void Log(string message)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
    }
}
