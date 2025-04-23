using TypeSafePipeline.lib.Command;
using TypeSafePipeline.lib.Pipeline;
using TypeSafePipeline.lib.Respones;
using TypeSafePipelint.Console.Test.Infra;
// 유효성 검사 핸들러 - 제네릭 명령용
public class ValidationHandler<TCommand, TResponse> : IPipelineHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    private readonly IValidator<TCommand> _validator;

    public ValidationHandler(IValidator<TCommand> validator)
    {
        _validator = validator;
    }

    public async Task<IResponse<TResponse>> HandleAsync(TCommand command, Func<Task<IResponse<TResponse>>> next)
    {
        var validationResult = await _validator.ValidateAsync(command);

        if (!validationResult.IsValid)
        {
            return Response<TResponse>.Failure(validationResult.ErrorMessage ?? "유효성 검사 실패");
        }

        return await next();
    }
}