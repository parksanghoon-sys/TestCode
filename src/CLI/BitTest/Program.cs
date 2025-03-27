// 사용 예제
class Program
{
    static void Main(string[] args)
    {
        // 3바이트 크기의 바이트 배열 생성
        byte[] data = new byte[3];
        Console.WriteLine($"초기 상태 (비트): {BitManipulator.ToBinaryString(data)}");
        Console.WriteLine();

        // 예제 1: 0번째 비트부터 3개 비트에 값 2 설정 (이진수: 010)
        BitManipulator.SetBits(data, 0, 3, 2);
        Console.WriteLine($"예제 1 결과 (16진수): {BitConverter.ToString(data)}");
        Console.WriteLine($"예제 1 결과 (비트): {BitManipulator.ToBinaryString(data)}");
        Console.WriteLine($"0-2번 비트 값: {BitManipulator.GetBits(data, 0, 3)} (이진수: {Convert.ToString(BitManipulator.GetBits(data, 0, 3), 2)})");
        Console.WriteLine();

        // 예제 2: 5번째 비트부터 2개 비트에 값 1 설정 (이진수: 01)
        BitManipulator.SetBits(data, 5, 2, 1);
        Console.WriteLine($"예제 2 결과 (16진수): {BitConverter.ToString(data)}");
        Console.WriteLine($"예제 2 결과 (비트): {BitManipulator.ToBinaryString(data)}");
        Console.WriteLine($"5-6번 비트 값: {BitManipulator.GetBits(data, 5, 2)} (이진수: {Convert.ToString(BitManipulator.GetBits(data, 5, 2), 2)})");
        Console.WriteLine();

        // 예제 3: 바이트 경계를 넘는 비트 설정 
        // 7번째 비트부터 9개 비트에 257 설정 (이진수: 100000001)
        BitManipulator.SetBits(data, 7, 9, 257);
        Console.WriteLine($"예제 3 결과 (16진수): {BitConverter.ToString(data)}");
        Console.WriteLine($"예제 3 결과 (비트): {BitManipulator.ToBinaryString(data)}");
        Console.WriteLine($"7-15번 비트 값: {BitManipulator.GetBits(data, 7, 9)} (이진수: {Convert.ToString(BitManipulator.GetBits(data, 7, 9), 2)})");
        Console.WriteLine();

        // 새로운 테스트 데이터로 초기화
        data = new byte[4] { 0, 0, 0, 0 };
        Console.WriteLine($"새 데이터 초기 상태 (비트): {BitManipulator.ToBinaryString(data)}");

        // 예제 4: 여러 바이트에 걸친 비트 설정
        BitManipulator.SetBits(data, 4, 12, 2730); // 이진수: 101010101010
        Console.WriteLine($"예제 4 결과 (16진수): {BitConverter.ToString(data)}");
        Console.WriteLine($"예제 4 결과 (비트): {BitManipulator.ToBinaryString(data)}");
        Console.WriteLine($"4-15번 비트 값: {BitManipulator.GetBits(data, 4, 12)} (이진수: {Convert.ToString(BitManipulator.GetBits(data, 4, 12), 2)})");

        // 비트 시각화 예제
        Console.WriteLine("\n비트 위치 시각화:");
        Console.WriteLine("비트 인덱스:  76543210 76543210 76543210");
        Console.WriteLine($"데이터 (비트): {BitManipulator.ToBinaryString(data)}");
    }
}