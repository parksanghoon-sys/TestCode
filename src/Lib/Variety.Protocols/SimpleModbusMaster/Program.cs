using Protocols.Abstractions.Logging;

internal class Program
{
    private static void Main(string[] args)
    {
        var logger = new ConsoleChannelLogger();
    }
}