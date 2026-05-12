using CleanProtocolSample.Protocol;

namespace CleanProtocolSample.WorkFlow;

/// <summary>
/// workflow 실행 컨텍스트.
/// </summary>
internal sealed class WorkflowContext<TState>
{
    public TState State { get; }

    public IProtocolClient Client { get; }

    public WorkflowContext(
        TState state,
        IProtocolClient client)
    {
        State = state;
        Client = client;
    }
}