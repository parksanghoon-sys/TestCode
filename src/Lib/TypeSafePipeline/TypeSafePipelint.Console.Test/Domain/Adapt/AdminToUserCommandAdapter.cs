using TypeSafePipeline.lib.Command;
using TypeSafePipeline.lib.Respones;
using TypeSafePipelint.Console.Test.Domain.Commands;
using TypeSafePipelint.Console.Test.Domain.Models;

namespace TypeSafePipelint.Console.Test.Domain.Adapt
{
    // 관리자 명령을 사용자 명령으로 처리하는 어댑터
    public class AdminToUserCommandAdapter : ICommandAdapter<CreateAdminCommand, ICommand<User>, Admin, User>
    {
        public ICommand<User> Adapt(CreateAdminCommand command)
        {
            // 관리자 명령을 사용자 명령으로 변환
            return new CreateUserCommand(command.Name, command.Email);
        }

        public IResponse<User> Adapt(IResponse<Admin> response)
        {
            if (!response.IsSuccess || response.Data == null)
            {
                return Response<User>.Failure(response.Message ?? "변환 실패", response.Exception);
            }

            // Admin은 User의 하위 타입이므로 이미 User 타입임
            return Response<User>.Success(response.Data, response.Message);
        }
    }
}
