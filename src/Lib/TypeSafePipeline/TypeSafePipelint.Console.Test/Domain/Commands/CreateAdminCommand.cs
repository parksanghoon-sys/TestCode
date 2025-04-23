using TypeSafePipeline.lib.Command;
using TypeSafePipeline.lib.Respones;
using TypeSafePipelint.Console.Test.Domain.Models;

namespace TypeSafePipelint.Console.Test.Domain.Commands
{
    // 관리자 생성 명령
    public class CreateAdminCommand : ICommand<Admin>
    {
        public string Name { get; }
        public string Email { get; }
        public string Role { get; }

        public CreateAdminCommand(string name, string email, string role)
        {
            Name = name;
            Email = email;
            Role = role;
        }

        async Task<IResponse> TypeSafePipeline.lib.Command.ICommand.ExecuteAsync()
        {
            // ICommand 인터페이스 구현을 위한 어댑터 메서드
            var response = await ExecuteAsync();
            return response;
        }

        public async Task<IResponse<Admin>> ExecuteAsync()
        {
            try
            {
                await Task.Delay(100); // 비동기 작업 시뮬레이션

                var newAdmin = new Admin(
                    Id: new Random().Next(1000, 9999),
                    Name: Name,
                    Email: Email,
                    Role: Role
                );

                return Response<Admin>.Success(newAdmin, "관리자가 성공적으로 생성되었습니다.");
            }
            catch (Exception ex)
            {
                return Response<Admin>.Failure("관리자 생성 중 오류가 발생했습니다.", ex);
            }
        }
    }
}
