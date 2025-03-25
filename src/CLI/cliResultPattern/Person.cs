
// 사용자 정의 클래스 예제
public record Person(string Name, int Age)
{
    public override string ToString()
    {
        return $"{Name} ({Age}세)";
    }
}
