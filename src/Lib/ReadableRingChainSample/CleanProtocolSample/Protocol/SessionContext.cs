using CleanProtocolSample.Logging;
using CleanProtocolSample.Transprot;

namespace CleanProtocolSample.Protocol;

internal class SessionContext
{
    private readonly IAppLogger _appLogger;
    private readonly ITransport _transport;
    public MessageQueueHub Queues { get; }

    public IAppLogger Logger { get => _appLogger; }

    public ITransport Transport { get => _transport; }

    public SessionContext(MessageQueueHub queues, IAppLogger logger, ITransport transport)
    {        
        Queues = queues;
        _transport = transport;
        _appLogger = logger;
    }
}