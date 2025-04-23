namespace TypeSafePipelint.Console.Test.Infra
{
    // 유효성 검사기 인터페이스
    public interface IValidator<in T>
    {
        Task<ValidationResult> ValidateAsync(T instance);
    }
}
