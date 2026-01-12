using System.Text;
using TcpSimulator.Domain.ValueObjects;

namespace TcpSimulator.Domain.Entities;

public sealed class TextMessage : MessageBase
{
    public string Text { get; }

    public TextMessage(string text)
        : base(MessageType.Text, Encoding.UTF8.GetBytes(text))
    {
        Text = text;
    }

    public TextMessage(ReadOnlyMemory<byte> payload)
        : base(MessageType.Text, payload)
    {
        Text = Encoding.UTF8.GetString(payload.Span);
    }
}
