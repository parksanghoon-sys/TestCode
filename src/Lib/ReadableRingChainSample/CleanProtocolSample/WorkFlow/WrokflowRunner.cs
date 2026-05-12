using CleanProtocolSample.Common;
using CleanProtocolSample.Logging;
using CleanProtocolSample.Protocol;

namespace CleanProtocolSample.WorkFlow;

/// <summary>
/// generic workflow engine.
///
/// 역할:
/// - step 실행
/// - next 이동
/// - workflow orchestration
/// </summary>
internal sealed class WrokflowRunner<TState>
{
    /// <summary>
    /// step 저장소.
    /// </summary>
    private readonly Dictionary<string, IWorkflowStep<TState>> _steps = new();
    /// <summary>
    /// protocol client.
    /// </summary>
    private readonly IProtocolClient _client;
    /// <summary>
    /// logger.
    /// </summary>
    private readonly IAppLogger _logger;
    public WrokflowRunner(IProtocolClient protocolClient, IAppLogger logger)
    {
        _client = protocolClient;
        _logger = logger;
    }
    /// <summary>
    /// workflow step 등록.
    /// </summary>
    public void AddStep(IWorkflowStep<TState> step)
    {
        _steps.Add(step.Name, step);
    }
    /// <summary>
    /// workflow 실행.
    /// </summary>
    public async Task<Result<TState>> RunAsync(string startStep, TState initialState, CancellationToken cancellationToken)
    {
        var context = new WorkflowContext<TState>(initialState, _client, _logger);

        var current = startStep;

        while (string.IsNullOrWhiteSpace(current) == false)
        {
            if (_steps.TryGetValue(current, out var step) == false)
            {
                return Result<TState>.Fail(
                  ErrorCodes.StepNotFound,
                  $"Step not found : {current}");
            }

            _logger.Info(
               $"STEP START : {current}");

            var result = await step.ExecuteAsync(context, cancellationToken);

            if (result.Result.IsFailure)
                return Result<TState>.Fail(result.Result.ErrorCode, result.Result.ErrorMessage);

            context.State = result.State;

            current = result.Next;
        }

        return Result<TState>.Success(context.State);
    }

}