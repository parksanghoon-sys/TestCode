using Protocols.Abstractions.Channels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Protocols.Abstractions.Logging
{
    public class UnrecognizedErrorLog : ChannelLog
    {
        public UnrecognizedErrorLog(IChannel channel, byte[] rawMessage)
            :base(channel)
        {
            RawMessage = rawMessage ?? new byte[0];
        }
        public IReadOnlyList<byte> RawMessage { get; }

        public override string ToString()
        {
            return RawMessage != null && RawMessage.Count > 0 ? $"Error Message: {BitConverter.ToString(RawMessage as byte[]).Replace('-', ' ')}" : base.ToString();
        }
    }
}
