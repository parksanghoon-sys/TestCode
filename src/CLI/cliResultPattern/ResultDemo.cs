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
        return Result.Success(new Person(parts[0].Trim(), age));
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

        var team = new Team(lines[0].Trim(), new List<Person>());

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
