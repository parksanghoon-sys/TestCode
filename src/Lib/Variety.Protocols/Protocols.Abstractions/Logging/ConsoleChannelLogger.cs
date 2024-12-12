using System;
using System.Collections.Generic;
using System.Text;

namespace Protocols.Abstractions.Logging
{
    /// <summary>
    /// Console 기반 Logger
    /// </summary>
    public class ConsoleChannelLogger : IChannelLogger
    {
        /// <summary>
        /// 통신 채널 Log 기록
        /// </summary>
        /// <param name="log"></param>
        public void Log(ChannelLog log)
        {
            Console.WriteLine($"({log.ChnnelDescription}) {log}");
        }
    }
}
