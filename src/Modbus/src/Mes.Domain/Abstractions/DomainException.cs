namespace Mes.Domain.Abstractions;

/// <summary>
/// 도메인 규칙 위반을 표현하는 예외입니다.
/// </summary>
/// <param name="message">도메인 규칙 위반 메시지입니다.</param>
public sealed class DomainException(string message) : InvalidOperationException(message);
