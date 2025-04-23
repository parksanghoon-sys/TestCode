using TypeSafePipeline.lib.Command;
using TypeSafePipeline.lib.Respones;

namespace TypeSafePipelint.Console.Test
{
    // 간단한 메디에이터 인터페이스
    public interface IMediator
    {
        Task<IResponse> SendAsync(ICommand command);
        Task<IResponse<TResponse>> SendAsync<TResponse>(ICommand<TResponse> command);
    }
}
