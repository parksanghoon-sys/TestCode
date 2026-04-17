namespace Mes.Client.Wpf.OperatorExecution;

/// <summary>
/// 스테이션 셸이 작업자에게 표시할 메시지를 나타냅니다.
/// </summary>
/// <param name="Title">메시지 제목입니다.</param>
/// <param name="Detail">메시지 상세 설명입니다.</param>
/// <param name="Severity">메시지 심각도 식별자입니다.</param>
public sealed record StationUserMessage(
    string Title,
    string Detail,
    string Severity);
