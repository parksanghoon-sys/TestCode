namespace TypeSafePipelint.Console.Test.Domain.Models
{
    // 도메인 모델 기본 클래스
    public abstract record Person(string Name, string Email);

    // 사용자 모델
    public record User(int Id, string Name, string Email) : Person(Name, Email);

    // 관리자 모델
    public record Admin(int Id, string Name, string Email, string Role) : User(Id, Name, Email);
}