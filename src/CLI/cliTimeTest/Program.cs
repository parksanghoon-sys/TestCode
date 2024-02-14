class Program

{
    [STAThread]
    static void Main(string[] args)
    {
        Program program = new Program();
        var date = program.ParsingTODDateFormat(25, 366, 5, 2, 2, 1, 4);
        var EgiTod = date;
        Console.WriteLine(EgiTod);
        var test = NotRuleConverter(6, 16, 5);
        test = NotRuleConverter(768, 90, 10);
        Console.WriteLine(test);

    }
    private string ParsingTODDateFormat(double year, double additionDay, double hour, double minute, double second, double hundredMilisecond, double tenMilisecond)
    {
        var todYear = (int)year + 2000;

        var additianDays = additionDay;
        DateTime date = new DateTime(todYear, 1, 1).AddDays((additianDays -1)% 366);
        var todMonth = date.Month;
        var todDay = date.Day;
        var todHour = (int)hour % 24;
        var todMinute = (int)minute % 60;
        var todSencond = (int)second % 61;
        var todMiliSecond = ((tenMilisecond % 10).ToString() + (hundredMilisecond % 10).ToString());

        var datetimeFormatString = new DateTime(todYear, todMonth, todDay).ToString("yyyy-MM-dd");
        datetimeFormatString += " " + todHour.ToString("00") + ":" + todMinute.ToString("00") + ":" + todSencond.ToString("00") + ":" + todMiliSecond;
        return datetimeFormatString;
    }
    public static double NotRuleConverter(double value, int maxValue, int length)
    {
        double bearingValue = 0;
        int originToInt = Convert.ToInt32(value);
        string bearingBinary = Convert.ToString(originToInt, 2).PadLeft(length, '0');
        var binarychars = bearingBinary.ToCharArray();

        for (int index = 0; index < length; index++)
        {
            if (binarychars[index] == '1')
            {
                bearingValue += ((maxValue ) / Math.Pow(2, index));
            }
        }

        return bearingValue;
    }
}