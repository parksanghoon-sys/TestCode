using cliConverterValueBytesToBit;

class Program
{
    private static void Main(string[] args)
    {
        IValueToByteConverter converter = new BitHelper4();
        byte[] bytes = new byte[2];

        converter.FillBits(bytes, 2, 6, 2, false);
        converter.FillBits(bytes, 10, 6, 1, false);
        
        Console.WriteLine($"{ByteToHexString(bytes)}");
        var data1 = converter.GetBits(bytes, 2, 6, false);
        var data2 = converter.GetBits(bytes, 10, 6, false);

        Console.WriteLine($"{data1}   {data2}");
    }
    private static string ByteToHexString(byte[] bytes)
    {
        return string.Join(" ", bytes.Select(b => b.ToString("X2")));
    }
}