using System.Text.RegularExpressions;

internal class Program
{
    private static void Main(string[] args)
    {
        // 예제 테스트
        Console.WriteLine(IsValidFrequency("118.296")); // True
        
        Console.WriteLine(IsValidFrequency("399.975")); // True
        Console.WriteLine(IsValidFrequency("399.975")); // True
        Console.WriteLine(IsValidFrequency("A99.975")); // True        
        Console.WriteLine(IsValidFrequency("B00.575")); // True
        Console.WriteLine(IsValidFrequency("106.500")); // False
        Console.WriteLine(IsValidFrequency("C00.000")); // False
        Console.WriteLine(IsValidFrequency("155.999")); // false
        Console.WriteLine(IsValidFrequency("B00.935")); // False
        Console.WriteLine("Hello, World!");
    }
    public static bool IsValidFrequency(string input)
    {
        string pattern = @"
        ^(?:
        (1[1-4][0-9]|15[0-4])\.(?:[0-9][0-9][0-9])$|       # 118.000~154.975
        (155)\.(?:[0-9][0-7][05])$|                        # 155.000~155.975
        (2[5-9][5-9]|3[0-9][0-8])\.(?:[0-9][0-9][0-9])$|   # 255.000~398.999
        (399)\.(?:[0-9][0-7][0-5])$|                       # 399.000~399.975
        (A[0-9][0-8])\.(?:[0-9][0-9][0-9])$|               # A00.000~A98.999
        (A99)\.(?:[0-9][0-7][05])$|                        # A99.000~A99.975
        B00\.(?:[0-8][0-9][0-9])$ |                       # B00.000~B00.875
        B00\.(?:9[0-2][05])$                            # B00.900~B00.925
        )$";

        return Regex.IsMatch(input, pattern, RegexOptions.IgnorePatternWhitespace);
    }
}