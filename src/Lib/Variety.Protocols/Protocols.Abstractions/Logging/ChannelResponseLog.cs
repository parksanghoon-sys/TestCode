using Protocols.Abstractions.Channels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Protocols.Abstractions.Logging
{
    /// <summary>
    /// 통신 채널을 통해 주고 받은 응답 메시지에 대한 Log
    /// </summary>
    public class ChannelResponseLog : ChannelMessageLog
    {
        private readonly IChannel _channel;
        private readonly IResponse _response;

        public ChannelResponseLog(IChannel channel, IResponse response, byte[] rawMessage, ChannelRequestLog requestLog)
            :base(channel, response, rawMessage)
        {
            _channel = channel;
            _response = response;
        }
        public override string ToString()
        {
            return $"Response : {base.ToString()}";
        }

    }
}
