using ReadableRingChainSample.Core;

namespace ReadableRingChainSample.Abstractions;

public interface ICommandTransport<TCommnad, TResponse>
{
    Task<Result> SendAsync(TCommnad command, CancellationToken cancellationToken = default);
    Task<Result<TResponse>> ReceiveAsync(CancellationToken cancellationToken = default);
}
