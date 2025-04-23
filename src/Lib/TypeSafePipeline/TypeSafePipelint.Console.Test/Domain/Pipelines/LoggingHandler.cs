using TypeSafePipeline.lib.Command;
using TypeSafePipeline.lib.Pipeline;
using TypeSafePipeline.lib.Respones;
using TypeSafePipelint.Console.Test.Infra;

namespace TypeSafePipelint.Console.Test.Domain.Pipelines
{
    // 로깅 핸들러 - 기본 명령용
    public class LoggingHandler<TCommand> : IPipelineHandler<TCommand>
        where TCommand : ICommand
    {
        private readonly ILogger _logger;

        public LoggingHandler(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<IResponse> HandleAsync(TCommand command, Func<Task<IResponse>> next)
        {
            _logger.Log($"명령 실행 전: {command.GetType().Name}");

            var response = await next();

            _logger.Log($"명령 실행 후: {command.GetType().Name}, 성공 여부: {response.IsSuccess}");

            return response;
        }
    }
}
// 로깅 핸들러 - 제네릭 명령용
public class LoggingHandler<TCommand, TResponse> : IPipelineHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    private readonly ILogger _logger;

    public LoggingHandler(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<IResponse<TResponse>> HandleAsync(TCommand command, Func<Task<IResponse<TResponse>>> next)
    {
        _logger.Log($"명령 실행 전: {command.GetType().Name}");

        var response = await next();

        _logger.Log($"명령 실행 후: {command.GetType().Name}, 성공 여부: {response.IsSuccess}");

        return response;
    }
}
