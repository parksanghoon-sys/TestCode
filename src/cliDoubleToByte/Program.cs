static class  Program
{
    [STAThread]
    static void Main(string[] args)
    {
        double d2 = 0;
        double d4 = 1;
        var modeCCodeA = Program.BoolArrayToByte(Program.DoubleValueToBool(
                         new double[] { 0, d2, d4}));
        Console.WriteLine(modeCCodeA.ToString());
    }
    public static byte BoolArrayToByte(this bool[] boolArray)
    {
        if (boolArray.Length > 8)
        {
            throw new ArgumentException("Input bool array cannot have more than 8 elements.");
        }

        byte resultByte = 0;

        for (int i = 0; i < boolArray.Length; i++)
        {
            if (boolArray[i])
            {
                resultByte |= (byte)(1 << i);
            }
        }
        return resultByte;
    }
    public static bool[] DoubleValueToBool(double[] value)
    {
        List<bool> result = new List<bool>(value.Length);
        foreach (var v in value)
        {
            result.Add(v == 1);
        }
        return result.ToArray();
    }
}