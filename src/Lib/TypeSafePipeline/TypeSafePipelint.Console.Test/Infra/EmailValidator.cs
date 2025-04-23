using TypeSafePipelint.Console.Test.Domain.Commands;

namespace TypeSafePipelint.Console.Test.Infra
{
    public class EmailValidator : IValidator<CreateUserCommand>
    {
        public Task<ValidationResult> ValidateAsync(CreateUserCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.Name))
                return Task.FromResult(new ValidationResult(false, "이름 누락"));
            if (string.IsNullOrWhiteSpace(command.Email))
            {
                return Task.FromResult(new ValidationResult(false, "이메일은 필수 항목입니다."));
            }

            if (!command.Email.Contains('@'))
            {
                return Task.FromResult(new ValidationResult(false, "유효한 이메일 주소를 입력하세요."));
            }

            return Task.FromResult(new ValidationResult(true));
        }
    }
}
