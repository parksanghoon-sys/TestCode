using System.Diagnostics;
using System.Net.Http.Headers;
// 사용자 정의 클래스 예제
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }

    public override string ToString()
    {
        return $"{Name} ({Age}세)";
    }
}

public class Team
{
    public string Name { get; set; }
    public List<Person> Members { get; set; }

    public override string ToString()
    {
        return $"팀: {Name}, 인원: {Members.Count}명";
    }
}
// 결과 출력 헬퍼 메서드
public static class ResultExtensions
{
    public static void Display<T>(this IResult<T> result, string successPrefix = "성공")
    {
        if (result.IsSuccess)
        {
            Console.WriteLine($"{successPrefix}: {result.Value}");
        }
        else
        {
            Console.WriteLine($"실패: {result.ErrorMessage}");
        }
    }
}
// 데이터 프로세서 인터페이스
public interface IDataProcessor<in TInput, out TOutput>
{
    IResult<TOutput> Process(TInput input);
}
public class FunctionalProcessor<TInput, TOutput> : IDataProcessor<TInput, TOutput>
{
    private readonly Func<TInput, IResult<TOutput>> _processor;
    public FunctionalProcessor(Func<TInput, IResult<TOutput>> processFunc)
    {
        _processor = processFunc;
    }
    public FunctionalProcessor(Func<TInput,TOutput> func)
    {
        _processor = input => Result.Try(input, func);
    }
    public IResult<TOutput> Process(TInput input)
    {
        return _processor(input);
    }
}
public class ResultDemo
{ 
    public static IResult<int> ProcessNumber(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result.Failure<int>("입력이 비었다.");
        if(int.TryParse(input, out var result))
            return Result.Success<int>(result);

        return Result.Failure<int>($"'{input}'Not Convert");
    }
    public static IResult<Person> ProcessPerson(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result.Failure<Person>("입력이 비었다.");
        var parts = input.Split(',');
        if(parts.Length < 2)
            return Result.Failure<Person>("Invalid Input Format It should be in the format 'name, age'.\" ");
        if (!int.TryParse(parts[1].Trim(), out int age))
            return Result.Failure<Person>($"나이 '{parts[1]}'를 숫자로 변환할 수 없습니다.");
        return Result.Success(new Person
        {
            Name = parts[0].Trim(),
            Age = age
        });
    }
    // 컬렉션 처리 예제
    public static IResult<List<int>> ProcessNumberList(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result.Failure<List<int>>("입력이 비어 있습니다.");

        var parts = input.Split(',');
        var numbers = new List<int>();

        foreach (var part in parts)
        {
            if (!int.TryParse(part.Trim(), out int number))
                return Result.Failure<List<int>>($"'{part}'를 숫자로 변환할 수 없습니다.");

            numbers.Add(number);
        }

        return Result.Success(numbers);
    }

    // 복합 객체 처리 예제
    public static IResult<Team> ProcessTeam(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result.Failure<Team>("입력이 비어 있습니다.");

        var lines = input.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
            return Result.Failure<Team>("입력 형식이 잘못되었습니다. '팀이름\\n이름1,나이1\\n이름2,나이2...' 형식이어야 합니다.");

        var team = new Team { Name = lines[0].Trim(), Members = new List<Person>() };

        for (int i = 1; i < lines.Length; i++)
        {
            var personResult = ProcessPerson(lines[i]);
            if (personResult.IsFailure)
                return Result.Failure<Team>($"팀원 정보 처리 중 오류: {personResult.ErrorMessage}");

            team.Members.Add(personResult.Value);
        }

        return Result.Success(team);
    }
    public static IResult<string> RunDemo(string input, Func<int, int> customFunc)
    {
        var numberResult = ProcessNumber(input);
        if (numberResult.IsFailure)
            return Result.Failure<string>(numberResult.ErrorMessage);
        try
        {
            var transformedValue = customFunc(numberResult.Value);
            return Result.Success($"입력 '{input}'에 사용자 함수 적용 결과: {transformedValue}");
        }
        catch (Exception ex)
        {
            return Result.Failure<string>($"사용자 함수 실행 중 오류: {ex.Message}");
        }
    }
}

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