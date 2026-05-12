namespace CleanProtocolSample.WorkFlow;
/// <summary>
/// workflow step 공통 인터페이스.
///
/// 모든 step은 ExecuteAsync 구현 필요.
/// </summary>
internal interface IWorkflowStep<TState>
{    /// <summary>
     /// step 이름.
     /// </summary>
    string Name { get; }
    /// <summary>
    /// step 실행.
    /// </summary>
    Task<StepResult<TState>>ExecuteAsync(WorkflowContext<TState> context, CancellationToken cancellationToken);
}
