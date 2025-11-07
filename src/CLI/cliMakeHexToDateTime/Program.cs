
Console.WriteLine("FILETIME 16진수 값을 입력하세요 (예: 01dc2e94421c3513):");

string hexInput = Console.ReadLine();

if(long.TryParse(hexInput, System.Globalization.NumberStyles.HexNumber, null, out long fileTime))
{
    try
    {
        DateTime dateTime = DateTime.FromFileTimeUtc(fileTime);
        Console.WriteLine($"UTC 시간: {dateTime:yyyy-MM-dd HH:mm:ss.fff}");
        Console.WriteLine($"로컬 시간: {dateTime.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff}");
    }
    catch (ArgumentOutOfRangeException)
    {
        Console.WriteLine("유효하지 않은 FILETIME 값입니다.");
    }
}
else
{
    Console.WriteLine("16진수 입력이 올바르지 않습니다.");
}
