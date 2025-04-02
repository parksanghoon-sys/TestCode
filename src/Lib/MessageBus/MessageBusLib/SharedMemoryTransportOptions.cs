namespace MessageBusLib;
/// <summary>
/// 공유 메모리 전송 계층 구성 옵션
/// </summary>
public class SharedMemoryTransportOptions : ITransportOptions
{
    /// <summary>
    /// 버스 이름
    /// </summary>
    public string BusName { get; set; } = "default";

    /// <summary>
    /// 이름 접두사
    /// </summary>
    public string NamePrefix { get; set; } = "LightweightMessenger_";

    /// <summary>
    /// 최대 메시지 크기 (바이트)
    /// </summary>
    public int MaxMessageSize { get; set; } = 1024 * 1024; // 1MB

    /// <summary>
    /// 버퍼에 저장할 최대 메시지 수
    /// </summary>
    public int MaxMessageCount { get; set; } = 100;

    /// <summary>
    /// 메시지 읽기 간격 (밀리초)
    /// </summary>
    public int MessageReadInterval { get; set; } = 1000;

    /// <summary>
    /// 뮤텍스 이름
    /// </summary>
    public string MutexName => $"{NamePrefix}Mutex_{BusName}";

    /// <summary>
    /// 이벤트 이름
    /// </summary>
    public string EventName => $"{NamePrefix}Event_{BusName}";

    /// <summary>
    /// 메모리 매핑 파일 이름
    /// </summary>
    public string MemoryMappedFileName => $"{NamePrefix}MMF_{BusName}";

    /// <summary>
    /// 메모리 버퍼 크기
    /// </summary>
    public long BufferSize => MaxMessageSize * MaxMessageCount;
}
