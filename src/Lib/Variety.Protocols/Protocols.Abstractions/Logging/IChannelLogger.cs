using System;
using System.Collections.Generic;
using System.Text;

namespace Protocols.Abstractions.Logging
{
    public interface IChannelLogger
    {
        /// <summary>
        /// 통신 채널 Log 기록
        /// </summary>
        /// <param name="log">통신 채널 Log</param>
        void Log(ChannelLog log);
    }
}
