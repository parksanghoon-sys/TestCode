using ReadableRingChainSample.Infra;

namespace ReadableRingChainSample.Core;

public sealed record ChainContext<TState>
    (TState State, 
    IAppLogger Logger, 
    IDictionary<string, object?> Items)
{
    public ChainContext<TState> WithSate(TState newState)
        => this with { State = newState };
    public T? GetITems<T>(string key)
    {
        if(Items.TryGetValue(key, out var value) && value is T typed)
            return typed;
        return default;
    }
    public void SetItem(string key, object? value)
    {
        Items[key] = value;
    }
}
public sealed record StepExecution<TStae>(
    TStae Stae,
    string? NextStepName,
    bool IsCompleted = false);
