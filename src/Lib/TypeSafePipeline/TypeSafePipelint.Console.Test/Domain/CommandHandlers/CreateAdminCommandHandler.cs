using TypeSafePipeline.lib.Command;
using TypeSafePipeline.lib.Respones;
using TypeSafePipelint.Console.Test.Domain.Commands;
using TypeSafePipelint.Console.Test.Domain.Models;

namespace TypeSafePipelint.Console.Test.Domain.CommandHandlers
{
    // 관리자 생성 명령 핸들러
    public class CreateAdminCommandHandler : ICommandHandler<CreateAdminCommand, Admin>
    {
        public async Task<IResponse<Admin>> HandleAsync(CreateAdminCommand command)
        {
            System.Console.WriteLine("CreateAdminCommandHandler 실행 중...");
            return await command.ExecuteAsync();
        }
    }
}
