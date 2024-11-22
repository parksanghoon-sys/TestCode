using System.Text.RegularExpressions;

internal class Program
{
    private static void Main(string[] args)
    {
        // 예제 테스트
        Console.WriteLine(IsValidFrequency("118.025")); // True
        Console.WriteLine(IsValidFrequency("399.975")); // True
        Console.WriteLine(IsValidFrequency("A99.975")); // True
        Console.WriteLine(IsValidFrequency("B00.925")); // True
        Console.WriteLine(IsValidFrequency("106.500")); // False
        Console.WriteLine(IsValidFrequency("C00.000")); // False

        Console.WriteLine("Hello, World!");
    }
    public static bool IsValidFrequency(string input)
    {
        string pattern = @"^(?:
        (1[1-5][1-5]|155)\.(?:[0-9][0-7][05])$|         # 118.000~155.975
        (2[5-9][0-9]|3[0-9][0-9])\.(?:[0-9][0-7][05])$| # 255.000~399.975
        A([0-9][0-9])\.(?:[0-9][0-7][05])$|             # A00.000~A99.975
        B00\.(?:[0-9][0-2][05])$                        # B00.000~B00.925
        )$";

        return Regex.IsMatch(input, pattern, RegexOptions.IgnorePatternWhitespace);
    }
}