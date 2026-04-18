using System.Collections.ObjectModel;

namespace MilStd1553.Host.Models;

/// <summary>
/// Host 조회 모델에서 사용하는 telemetry 이벤트 유형입니다.
/// </summary>
public enum TelemetryEventType
{
    /// <summary>
    /// 메시지 프레임 이벤트입니다.
    /// </summary>
    MessageFrame = 0,

    /// <summary>
    /// 수동 버스 전환 이벤트입니다.
    /// </summary>
    BusSwitch = 1,

    /// <summary>
    /// 자동 failover 이벤트입니다.
    /// </summary>
    AutoFailover = 2,
}

/// <summary>
/// Host 관점의 메시지 프레임 조회 모델입니다.
/// </summary>
/// <param name="CommandWordRaw">원본 커맨드 워드입니다.</param>
/// <param name="StatusWordRaw">원본 상태 워드입니다.</param>
/// <param name="DataWords">원본 데이터 워드 목록입니다.</param>
/// <param name="BusLine">프레임이 흐른 버스 라인입니다.</param>
/// <param name="TimeTagMicros">프레임 time-tag입니다.</param>
public sealed record TelemetryMessageFrame(
    ushort CommandWordRaw,
    ushort? StatusWordRaw,
    ReadOnlyCollection<ushort> DataWords,
    BusLine BusLine,
    long TimeTagMicros);

/// <summary>
/// Host가 조회하는 telemetry 이벤트 레코드입니다.
/// </summary>
/// <param name="Type">이벤트 유형입니다.</param>
/// <param name="TimeTagMicros">이벤트 time-tag입니다.</param>
/// <param name="ActiveBus">이벤트 당시 활성 버스입니다.</param>
/// <param name="Description">사람이 읽을 수 있는 설명입니다.</param>
/// <param name="MessageFrame">메시지 이벤트일 때의 프레임입니다.</param>
public sealed record TelemetryEventRecord(
    TelemetryEventType Type,
    long TimeTagMicros,
    BusLine ActiveBus,
    string Description,
    TelemetryMessageFrame? MessageFrame);
