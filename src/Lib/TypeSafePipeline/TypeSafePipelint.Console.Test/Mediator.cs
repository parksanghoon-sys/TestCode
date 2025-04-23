using TypeSafePipeline.lib.Command;
using TypeSafePipeline.lib.Respones;
using TypeSafePipelint.Console.Test.Infra;

namespace TypeSafePipelint.Console.Test
{
    // 중재자 구현
    public class Mediator : IMediator
    {
        private readonly Dictionary<Type, object> _handlers = new();
        private readonly ILogger _logger;

        public Mediator(ILogger logger)
        {
            _logger = logger;
        }
        public void RegisterHandler<TCommand, TResponse>(ICommandHandler<TCommand, TResponse> handler)
            where TCommand : ICommand<TResponse>
        {
            _handlers[typeof(TCommand)] = handler;
        }

        public void RegisterHandler<TCommand>(ICommandHandler<TCommand> handler)
            where TCommand : ICommand
        {
            _handlers[typeof(TCommand)] = handler;
        }
        public async Task<IResponse> SendAsync(ICommand command)
        {
            var commandType = command.GetType();

            if (!_handlers.TryGetValue(commandType, out var handler))
            {
                return Response.Failure($"명령 {commandType.Name}에 대한 핸들러가 없습니다.");
            }
            try
            {
                _logger.Log($"명령 {commandType.Name} 메디에이터에서 처리 중...");

                // 리플렉션을 사용한 핸들러 호출
                dynamic dynamicHandler = handler;
                dynamic dynamicCommand = command;
                return await dynamicHandler.HandleAsync(dynamicCommand);
            }
            catch (Exception ex)
            {
                return Response.Failure($"명령 {commandType.Name} 처리 중 오류가 발생했습니다: {ex.Message}", ex);
            }
        }

        public async Task<IResponse<TResponse>> SendAsync<TResponse>(ICommand<TResponse> command)
        {
            var commandType = command.GetType();

            if (!_handlers.TryGetValue(commandType, out var handler))
            {
                return Response<TResponse>.Failure($"명령 {commandType.Name}에 대한 핸들러가 없습니다.");
            }

            try
            {
                _logger.Log($"명령 {commandType.Name} 메디에이터에서 처리 중...");

                // 리플렉션을 사용한 핸들러 호출
                dynamic dynamicHandler = handler;
                dynamic dynamicCommand = command;
                return await dynamicHandler.HandleAsync(dynamicCommand);
            }
            catch (Exception ex)
            {
                return Response<TResponse>.Failure($"명령 {commandType.Name} 처리 중 오류가 발생했습니다: {ex.Message}", ex);
            }
        }
    }
}
