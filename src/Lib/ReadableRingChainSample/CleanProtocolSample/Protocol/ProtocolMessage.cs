namespace CleanProtocolSample.Protocol;
/// <summary>
/// 프로토콜 메시지.
/// </summary>
internal sealed record ProtocolMessage(int CorrelationId, EMessageKind Kind, EResponseStatus Status, string Code, string Payload);
