

using cliompensatioinOfNegative2;
using System;

class Program
{
    static void Main(string[] args)
    {
        var testc = new BitCalculation();
        byte[] bytes = new byte[1] { 0x67};
        var test = testc.NegativePositiveConvert(53859,16);
        Console.WriteLine(test);
    }
}