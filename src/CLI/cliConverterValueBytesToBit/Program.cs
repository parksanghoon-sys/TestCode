using cliConverterValueBytesToBit;

class Program
{
    private static void Main(string[] args)
    {
        IConverter converter = new BitHelper4();
        byte[] bytes = new byte[2];

        converter.FillBits(bytes, 2, 6, 2, false);
        converter.FillBits(bytes, 10, 6, 1, false);

        foreach (byte b in bytes)
        {
            // 16진수(hex)로 출력
            Console.Write($"{b:X2} ");

            // 이진수(binary)로 출력
            Console.WriteLine(Convert.ToString(b, 2).PadLeft(8, '0'));
        }

    }
}