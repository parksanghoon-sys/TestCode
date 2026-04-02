using ReadableRingChainSample.Abstractions;
using System.Runtime.InteropServices;

namespace ReadableRingChainSample.Core;

public sealed class CommandStep<TState, TCommand, TResponse> : IChainStep<TState>
     where TState : notnull
     where TCommand : notnull
     where TResponse : notnull
{
    public string Name { get; } = string.Empty;
    private Func<ChainContext<TState>, CancellationToken, Task<Result<TCommand>>>? _commandFactory;
    private Func<ChainContext<TState>, TCommand, CancellationToken, Task<Result>>? _sender;
    private Func<ChainContext<TState>, TCommand, CancellationToken, Task<Result<TResponse>>>? _receiver;
    private Func<ChainContext<TState>, TCommand, TResponse, Result>? _validator;
    private Func<ChainContext<TState>, TCommand, TResponse, TState>? _stateUpdater;
    private Func<ChainContext<TState>, TCommand, TResponse, string?>? _nextStepSelector;
    private Func<ChainContext<TState>, TCommand, TResponse, bool>? _completionChecker;
    private int _retryCount;
    private TimeSpan? _timeout;
    public CommandStep(string name)
    {
        Name = name;
    }
    public CommandStep<TState, TCommand, TResponse> BuildCommand(
        Func<ChainContext<TState>, CancellationToken, Task<Result<TCommand>>> commandFactory)
    {
        _commandFactory = commandFactory;
        return this;
    }
    public CommandStep<TState, TCommand, TResponse> SendBy(
        Func<ChainContext<TState>, TCommand, CancellationToken, Task<Result>> sender)
    {
        _sender = sender;
        return this;
    }
    public CommandStep<TState, TCommand, TResponse> ReceiveBy(
        Func<ChainContext<TState>, TCommand, CancellationToken, Task<Result<TResponse>>> receiver)
    {
        _receiver = receiver;
        return this;
    }
    public CommandStep<TState, TCommand, TResponse> ValidateBy(
    Func<ChainContext<TState>, TCommand, TResponse, Result> validator)
    {
        _validator = validator;
        return this;
    }

    public CommandStep<TState, TCommand, TResponse> UpdateStateBy(
        Func<ChainContext<TState>, TCommand, TResponse, TState> updater)
    {
        _stateUpdater = updater;
        return this;
    }

    public CommandStep<TState, TCommand, TResponse> GoTo(
        Func<ChainContext<TState>, TCommand, TResponse, string?> nextSelector)
    {
        _nextStepSelector = nextSelector;
        return this;
    }

    public CommandStep<TState, TCommand, TResponse> CompleteWhen(
        Func<ChainContext<TState>, TCommand, TResponse, bool> completionChecker)
    {
        _completionChecker = completionChecker;
        return this;
    }
    public CommandStep<TState, TCommand, TResponse>WithRetry(int retryCount)
    {
        _retryCount = retryCount;
        return this;
    }
    public CommandStep<TState, TCommand, TResponse> WithTimeout(TimeSpan timeout)
    {
        _timeout = timeout <= TimeSpan.Zero ? null : timeout;
        return this;
    }
    public async Task<Result<StepExecution<TState>>> ExcuteAsync(ChainContext<TState> context, CancellationToken cancellationToken = default)
    {
        if(IsConfigured() == false)
            return Result<StepExecution<TState>>.Failure("STEP_NOT_CONFIGURED",$"Step '{Name}' is not fully configured.");

        var attempCount = _retryCount + 1;
        Result<StepExecution<TState>>? lastFailure = null;

        for(var attemp = 1; attemp <= attempCount; attemp++)
        {
            var result = await ExcuteCoreAsync(context, attemp, cancellationToken);
            if (result.IsSuccess)
                return result;

            lastFailure = result;
            if(attemp < attempCount)
                context.Logger.Warning($"Step '{Name}' will retry. attempt={attemp}, error={result}");
        }        
        return lastFailure ?? Result<StepExecution<TState>>.Failure("STEP_EXECUTION_FAILED", $"Step '{Name}' failed after {attempCount} attempts.");
    }
    private bool IsConfigured()
    {
        return _commandFactory is not null
              && _sender is not null
              && _receiver is not null
              && _validator is not null
              && _stateUpdater is not null
              && _nextStepSelector is not null;
    }
    private async Task<Result<StepExecution<TState>>> ExcuteCoreAsync(
        ChainContext<TState> context, 
        int attempt,
        CancellationToken cancellationToken)
    {
        using var timeoutCtr = _timeout is null ? null : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        if (timeoutCtr is not null)
        {
            timeoutCtr.CancelAfter(_timeout.Value);
        }
        var effectiveToken = timeoutCtr?.Token ?? cancellationToken;
        try
        {
            context.Logger.Info($"Step '{Name}' started. attempt={attempt}");

            var commandResult = await _commandFactory!(context, effectiveToken);

            if(commandResult.IsFailure || commandResult.Value is null)
            {
                context.Logger.Error($"Step '{Name}' failed to build command. attempt={attempt}");
                return Result<StepExecution<TState>>.Failure(commandResult.ErrorCode, commandResult.ErrorMessage);
            }

            var command = commandResult.Value;
            context.Logger.Info($"Step '{Name}' command built.");

            var sendResult = await _sender!(context, command, effectiveToken);
            if(sendResult.IsFailure)
            {
                context.Logger.Error($"Step '{Name}' failed to send command. attempt={attempt}");
                return Result<StepExecution<TState>>.Failure(sendResult.ErrorCode, sendResult.ErrorMessage);
            }
            context.Logger.Info($"Step '{Name}' command sent.");

            var receiveResult = await _receiver!(context, command, effectiveToken);
            if (receiveResult.IsFailure || receiveResult.Value is null)
                return Result<StepExecution<TState>>.Failure(receiveResult.ErrorCode, receiveResult.ErrorMessage);

            var response = receiveResult.Value;
            context.Logger.Info($"Step '{Name}' response received.");

            var validateResult = _validator!(context, command, response);
            if (validateResult.IsFailure)
                return Result<StepExecution<TState>>.Failure(validateResult.ErrorCode, validateResult.ErrorMessage);

            var newState = _stateUpdater!(context, command, response);
            var isCompleted = _completionChecker?.Invoke(context, command, response) ?? false;
            var nextStep = isCompleted ? null : _nextStepSelector!(context, command, response);

            context.Logger.Info(
                $"Step '{Name}' completed. next={(isCompleted ? "<completed>" : nextStep)}");

            return Result.Success(new StepExecution<TState>(newState, nextStep, isCompleted));
        }
        catch (OperationCanceledException) when (_timeout is not null && !cancellationToken.IsCancellationRequested)
        {
            return Result<StepExecution<TState>>.Failure(
                "STEP_TIMEOUT",
                $"Step '{Name}' timed out after {_timeout.Value.TotalMilliseconds} ms.");
        }
        catch (Exception ex)
        {
            return Result<StepExecution<TState>>.Failure(
                "STEP_EXCEPTION",
                $"Step '{Name}' threw exception: {ex.Message}");
        }
        return null;
    }

  
}
