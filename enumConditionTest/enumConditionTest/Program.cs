public class Program
{
    public enum CombinedControl { Value1 = 1, Value2, Value3, Value4, Value5, Value6, Value7 };
    public enum DownAdvisory { Value1 = 1, Value2, Value3, Value4, Value5 };
    public enum VerticalControl { Value1 = 1, Value2, Value3, Value4, Value5 };
    public enum UpAdvisory { Value1 = 1, Value2, Value3, Value4, Value5 };

    public static void Main(string[] args)
    {
        double input = 79; // 전달받은 short 값

        var intdata = Convert.ToInt32(input);
        bool isNegative = (intdata & (1 << 6)) != 0; // 7번째 비트 체크
        int number = (int)(intdata & ((1 << 6) - 1)); // 하위 6비트로 숫자 표현


        int valuetest = 0;
        var test = new Dictionary<(CombinedControl, DownAdvisory, VerticalControl, UpAdvisory), Action>()
        {
            { (CombinedControl.Value1, DownAdvisory.Value2, VerticalControl.Value4, UpAdvisory.Value1), () =>
            {
                Console.WriteLine("Special case1");
                valuetest = 1;
            }},
            { (CombinedControl.Value4, DownAdvisory.Value2, VerticalControl.Value1, UpAdvisory.Value2), () =>
            {
                Console.WriteLine("Special case2");
                valuetest = 2;
            } },
            { (CombinedControl.Value1, DownAdvisory.Value3, VerticalControl.Value4, UpAdvisory.Value3), () =>
            {
                Console.WriteLine("Special case3");
                valuetest = 3;
            } },
            { (CombinedControl.Value4, DownAdvisory.Value2, VerticalControl.Value4, UpAdvisory.Value5), () => Console.WriteLine("Special case4")},
        };
        var specicalCase = new HashSet<string>()
        {
            "CombinedControl.Value1,DownAdvisory.Value2,VerticalControl.Value4,UpAdvisory.Value1",
            "CombinedControl.Value4,DownAdvisory.Value2,VerticalControl.Value1,UpAdvisory.Value2",
            "CombinedControl.Value1,DownAdvisory.Value3,VerticalControl.Value4,UpAdvisory.Value3",
            "CombinedControl.Value4,DownAdvisory.Value2,VerticalControl.Value4,UpAdvisory.Value5",
        };
        CombinedControl cc = CombinedControl.Value4;
        DownAdvisory da = DownAdvisory.Value2;
        VerticalControl vc = VerticalControl.Value1;
        UpAdvisory ua = UpAdvisory.Value2;

        var caseString = $"{cc},{da},{vc},{ua}";  // 데이터를 문자열 형태로 변환합니다.

        //if (specicalCase.Contains(caseString))  // 특별한 처리를 해야 하는 경우인지 확인합니다.
        //{
        //    // 특별한 처리를 수행합니다.
        //    Console.WriteLine($"Special case: {caseString}");
        //}
        var key = (cc, da, vc, ua);  // 데이터를 키로 변환합니다.

        if (test.TryGetValue(key, out var specialAction))  // 특별한 처리를 해야 하는 경우인지 확인합니다.
        {
            // 특별한 처리를 수행합니다.
            specialAction();
        }

        Console.ReadLine();
    }
}