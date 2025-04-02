using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBusLib.Exceptions;

/// <summary>
/// 메시지 버스 예외 기본 클래스
/// </summary>
public class MessageBusException : Exception
{
    public MessageBusException(string message) : base(message) { }
    public MessageBusException(string message, Exception innerException) : base(message, innerException) { }
}
