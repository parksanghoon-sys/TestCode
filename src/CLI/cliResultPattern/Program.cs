using System.Diagnostics;
using System.Net.Http.Headers;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("===== 다양한 데이터 타입을 지원하는 Result 패턴 데모 =====\n");

        // 1. 기본 타입(int) 처리 테스트
        Console.WriteLine("1. 기본 타입(int) 처리:");
        ResultDemo.ProcessNumber("42").Display("숫자 처리 결과");
        ResultDemo.ProcessNumber("abc").Display("숫자 처리 결과");

        // 2. 사용자 정의 클래스 처리 테스트
        Console.WriteLine("\n2. 사용자 정의 클래스(Person) 처리:");
        ResultDemo.ProcessPerson("홍길동, 30").Display("사람 처리 결과");
        ResultDemo.ProcessPerson("김철수").Display("사람 처리 결과");

        // 3. 컬렉션 처리 테스트
        Console.WriteLine("\n3. 컬렉션(List<int>) 처리:");
        ResultDemo.ProcessNumberList("1, 2, 3, 4, 5").Display("숫자 목록 처리 결과");
        ResultDemo.ProcessNumberList("1, 2, x, 4, 5").Display("숫자 목록 처리 결과");

        // 4. 복합 객체 처리 테스트
        Console.WriteLine("\n4. 복합 객체(Team) 처리:");
        var teamData = "개발팀\n홍길동, 30\n김철수, 28\n이영희, 32";
        var teamResult = ResultDemo.ProcessTeam(teamData);
        teamResult.Display("팀 처리 결과");

        if (teamResult.IsSuccess)
        {
            Console.WriteLine("팀 멤버 목록:");
            foreach (var member in teamResult.Value.Members)
            {
                Console.WriteLine($"  - {member}");
            }
        }

        // 5. 사용자 정의 함수 적용 테스트
        Console.WriteLine("\n5. 사용자 정의 함수 적용:");
        // 제곱 함수
        ResultDemo.RunDemo("7", x => x * x).Display("제곱 함수 적용");
        // 팩토리얼 함수
        ResultDemo.RunDemo("5", x => Factorial(x)).Display("팩토리얼 함수 적용");
        // 사용자 정의 변환 함수 (짝수/홀수 판별)
        ResultDemo.RunDemo("10", x => x % 2 == 0 ? 100 : 0).Display("짝수 판별 함수 적용");

        Console.ReadLine();
    }
    // 팩토리얼 계산 헬퍼 함수
    private static int Factorial(int n)
    {
        if (n < 0) throw new ArgumentException("음수의 팩토리얼은 계산할 수 없습니다.");
        if (n <= 1) return 1;
        return n * Factorial(n - 1);
    }
}