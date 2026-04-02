using ReadableRingChainSample.Abstractions;
using ReadableRingChainSample.Infra;

namespace ReadableRingChainSample.Core;

public sealed class ChainRunner<TState>
{
    private readonly Dictionary<string, IChainStep<TState>> _steps = new(StringComparer.OrdinalIgnoreCase);
    private readonly IAppLogger _logger;

    public ChainRunner(IAppLogger logger)
    {
        _logger = logger;
    }
    public ChainRunner<TState> Add(IChainStep<TState> step)
    {
        if (_steps.TryAdd(step.Name, step) == false)
            throw new InvalidOperationException($"Step '{step.Name}' already exists.");

        return this;
    }
    public async Task<Result<TState>> RunAsync(
        string startStepName,
        TState initialState,
        int maxSteps = 100,
        CancellationToken cancellationToken = default)
    {
        if (_steps.ContainsKey(startStepName) == false)
            return Result<TState>.Failure("START_STEP_NOT_FOUND", $"Start step '{startStepName}' not found.");

        var context = new ChainContext<TState>(
            State: initialState,
            Logger: _logger,
            Items: new Dictionary<string, object?>());

        var currentStepName = startStepName;

        for (int i = 1; i <= maxSteps; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_steps.TryGetValue(currentStepName, out var step) == false)
                return Result<TState>.Failure("STEP_NOT_FOUND", $"Step '{currentStepName}' not found.");

            _logger.Info($"Runner executing step '{currentStepName}'. index={i}");

            var result = await step.ExcuteAsync(context, cancellationToken);

            if (result.IsFailure || result.Value is null)
            {
                _logger.Error($"Runner failed at step '{currentStepName}'. {result.ErrorCode}: {result.ErrorMessage}");
                return Result<TState>.Failure(result.ErrorCode, result.ErrorMessage);
            }

            context = context.WithSate(result.Value.Stae);

            if (result.Value.IsCompleted)
            {
                _logger.Info($"Runner completed at step '{currentStepName}'.");
                return Result<TState>.Success(context.State);
            }
            if (string.IsNullOrWhiteSpace(result.Value.NextStepName))
            {
                return Result<TState>.Failure(
                    "NEXT_STEP_EMPTY",
                    $"Step '{currentStepName}' did not specify a next step.");
            }

            currentStepName = result.Value.NextStepName!;
        }
        return Result<TState>.Failure("MAX_STEPS_EXCEEDED", $"Exceeded max steps: {maxSteps}");
    }
}
