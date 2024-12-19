using Protocols.Abstractions.Channels;
using Protocols.Abstractions.Logging;
using Protocols.Channels;

internal class Program
{
    private static void Main(string[] args)
    {
        var logger = new ConsoleChannelLogger();

        IChannel channel = new TcpChannelProvider(502) { Logger = logger };

        var modbusSalveService = new ModbusSl
    }
}