using System.Text.RegularExpressions;

internal class Program
{
    private static void Main(string[] args)
    {
        Program program = new Program();

        double data1 = 118.196;
        double data2 = 392.375;
        double data3 = 395.675;
        double data4 = 391.975;

        program.IsVaildSaturnVersion4(data1);
        program.IsVaildSaturnVersion4(data2);
        program.IsVaildSaturnVersion4(data3);
        program.IsVaildSaturnVersion4(data4);

        Console.WriteLine();
    }
    private bool IsVaildSaturnVersion4(double frequency)
    {
        var validFrequency = Math.Floor(frequency % 100 %10 * 10);
        if ((validFrequency >= 25 && validFrequency <= 49) || (validFrequency >= 75 && validFrequency <= 99))
        {
            return false;
        }
        return true;
    }
}