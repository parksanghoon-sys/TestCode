using System;
using TypeSafePipeline.lib.Command;
using TypeSafePipeline.lib.Respones;
using TypeSafePipelint.Console.Test.Domain.Commands;
using TypeSafePipelint.Console.Test.Domain.Models;

namespace TypeSafePipelint.Console.Test.Domain.CommandHandlers
{
    // 사용자 생성 명령 핸들러
    public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, User>
    {
        public async Task<IResponse<User>> HandleAsync(CreateUserCommand command)
        {            
            System.Console.WriteLine("CreateUserCommandHandler 실행 중...");
            
            return await command.ExecuteAsync();
        }
    }
}
