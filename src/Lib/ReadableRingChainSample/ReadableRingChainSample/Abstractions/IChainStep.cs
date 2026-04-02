using ReadableRingChainSample.Core;

namespace ReadableRingChainSample.Abstractions;

public interface IChainStep<TState>
{
    string Name { get; }
    Task<Result<StepExecution<TState>>> ExcuteAsync(ChainContext<TState> context, CancellationToken cancellationToken = default);
}
