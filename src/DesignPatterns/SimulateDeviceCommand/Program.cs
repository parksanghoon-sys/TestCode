using System.Threading;

internal class Program
{
    private async static Task Main(string[] args)
    {
        Console.WriteLine("=== 바이너리 프로토콜 장비 제어 시스템 ===");
        Console.WriteLine("프로토콜 구조: [CMD(1)] [LENGTH(1)] [DATA(N)] [CHECKSUM(2)]");
        Console.WriteLine();
        Console.WriteLine("사용된 디자인 패턴:");
        Console.WriteLine("- Command Pattern: 바이너리 명령 객체화");
        Console.WriteLine("- Chain of Responsibility: 순차 실행");
        Console.WriteLine("- State Pattern: 명령 상태 관리");
        Console.WriteLine("- Template Method: 공통 실행 흐름");
        Console.WriteLine("- Strategy Pattern: 바이너리 직렬화/재시도 전략");
        Console.WriteLine("- Function Pattern: 커맨드별 맞춤 검증 함수");
        Console.WriteLine("=".PadRight(50, '='));
        Console.WriteLine();

        var system = new DeviceControlSystem();

        Console.WriteLine("Enter를 눌러 바이너리 프로토콜 시퀀스를 시작하세요...");
        Console.ReadLine();

        await system.OnExecuteButtonClickAsync();

        Console.WriteLine("\n아무 키나 눌러 종료하세요...");
        Console.ReadKey();
    }
}
