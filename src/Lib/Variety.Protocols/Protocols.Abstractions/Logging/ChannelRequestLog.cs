using Protocols.Abstractions.Channels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Protocols.Abstractions.Logging
{
    /// <summary>
    /// 통신 채널을 통해 주고 받은 요청 메시지에 대한 Log
    /// </summary>
    public class ChannelRequestLog : ChannelMessageLog
    {
        private readonly IRequest _request;

        public ChannelRequestLog(IChannel channel, IRequest request, byte[] rawMessage)
            : base(channel, request, rawMessage)
        {
            _request = request;
        }
        public override string ToString()
        {
            return $"Request: {base.ToString()}";
        }
    }
}
