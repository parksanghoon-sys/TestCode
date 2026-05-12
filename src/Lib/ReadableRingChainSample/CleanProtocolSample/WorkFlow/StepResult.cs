namespace CleanProtocolSample.WorkFlow;

internal sealed record StepResult<TState>(TState State, string? NextStep, bool IsCompleted);
