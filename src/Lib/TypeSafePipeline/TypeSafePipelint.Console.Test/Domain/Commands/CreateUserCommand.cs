using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TypeSafePipeline.lib.Command;
using TypeSafePipeline.lib.Respones;
using TypeSafePipelint.Console.Test.Domain.Models;

namespace TypeSafePipelint.Console.Test.Domain.Commands
{
    // 사용자 생성 명령
    public class CreateUserCommand : ICommand<User>
    {
        public string Name { get; init; }
        public string Email { get;  init; }
        public CreateUserCommand(string naem , string email)
        {
            Name = naem;
            Email = email;
        }

        public async Task<IResponse<User>> ExecuteAsync()
        {
            try
            {
                await Task.Delay(100);

                var newUser = new User(
                    Id: new Random().Next(1000, 9999),
                    Name: Name,
                    Email: Email);

                return Response<User>.Success(newUser);
            }
            catch (Exception ex)
            {
                return Response<User>.Failure("사용자 생성 중 오류가 발생했습니다.", ex);                
            }
        }

        async Task<IResponse> TypeSafePipeline.lib.Command.ICommand.ExecuteAsync()
        {
            var response = await ExecuteAsync();
            return response;
        }
    }
}
