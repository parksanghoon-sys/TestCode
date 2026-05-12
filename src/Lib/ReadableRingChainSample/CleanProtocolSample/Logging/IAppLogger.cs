namespace CleanProtocolSample.Logging;

/// <summary>
/// Log 인터페이스
/// </summary>
internal interface IAppLogger
{
    void Info(string message);
    void Warn(string message);
    void Error(string message);
}
