namespace TypeSafePipelint.Console.Test.Infra
{
    // 유효성 검사 결과
    public record ValidationResult(bool IsValid, string? ErrorMessage = null);
}
