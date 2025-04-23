using TypeSafePipeline.lib.Command;
using TypeSafePipeline.lib.Respones;
using TypeSafePipelint.Console.Test.Domain.Models;

namespace TypeSafePipelint.Console.Test.Domain.Adapt
{
    // User를 Person 타입으로 변환하는 어댑터
    public class UserToPersonAdapter : ICommandAdapter<ICommand<User>, ICommand<Person>, User, Person>
    {
        public ICommand<Person> Adapt(ICommand<User> command)
        {
            return new UserCommandAdapter(command);
        }

        public IResponse<Person> Adapt(IResponse<User> response)
        {
            if (!response.IsSuccess || response.Data == null)
            {
                return Response<Person>.Failure(response.Message ?? "변환 실패", response.Exception);
            }

            return Response<Person>.Success(response.Data, response.Message);
        }

        // User 명령을 Person 명령으로 변환하는 내부 어댑터 클래스
        private class UserCommandAdapter : ICommand<Person>
        {
            private readonly ICommand<User> _userCommand;

            public UserCommandAdapter(ICommand<User> userCommand)
            {
                _userCommand = userCommand;
            }
 

            public async Task<IResponse<Person>> ExecuteAsync()
            {
                var userResponse = await _userCommand.ExecuteAsync();

                if (!userResponse.IsSuccess || userResponse.Data == null)
                {
                    return Response<Person>.Failure(userResponse.Message ?? "실행 실패", userResponse.Exception);
                }

                return Response<Person>.Success(userResponse.Data, userResponse.Message);
            }

            async Task<IResponse> ICommand.ExecuteAsync()
            {
                var respons = await ExecuteAsync();
                return respons;
            }
        }
    }
}
