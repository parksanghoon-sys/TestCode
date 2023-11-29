using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        foreach (var (lower, upper, bitPattern) in DumpCode())
        {
            Console.WriteLine($"{lower,10} to {upper,10}: {bitPattern}");
        }
    }

    static int Gillham(int code)
    {
        string bits = Convert.ToString(code, 2).PadLeft(12, '0');

        if (bits.Length != 12)
        {
            throw new ArgumentException("Expected 12-bit input word");
        }

        bool d1 = bits[0] == '1';

        int g500 = GrayToBinary(bits.Substring(0, 9));
        int g100 = GrayToBinary(bits.Substring(9));

        int[] validG100 = (g500 > 0) ? new int[] { 7, 4, 3, 2, 1 } : new int[] { 3, 4, 7 };

        if (d1 || Array.IndexOf(validG100, g100) == -1)
        {
            Console.WriteLine($"Gillham code {bits} is not valid {g500} {g100}");
            return -1;
        }

        g100 = (g100 == 7) ? 5 : g100;

        if (g500 % 2 == 1)
        {
            g100 = 6 - g100;
        }

        g100--;

        int g100Scale = 100;

        if (g500 == 0 && g100 == 2)
        {
            g100 = 3;
            g100Scale = 75;
        }

        return ((g500 * 500) + (g100 * g100Scale)) - 1200;
    }

    static int GrayToBinary(string bitString)
    {
        List<int> gray = new List<int>(Array.ConvertAll(bitString.ToCharArray(), c => int.Parse(c.ToString())));
        List<int> bits = new List<int> { gray[0] };

        for (int i = 1; i < gray.Count; i++)
        {
            bits.Add(bits[^1] ^ gray[i]);
        }

        return Convert.ToInt32(string.Join("", bits), 2);
    }

    static List<(int lower, int upper, string bitPattern)> DumpCode()
    {
        List<(int lower, int upper, string bitPattern)> code = new List<(int lower, int upper, string bitPattern)>();

        for (int i = 0; i < Math.Pow(2, 12); i++)
        {
            string bitPattern = Convert.ToString(i, 2).PadLeft(12, '0');

            int g;
            try
            {
                g = Convert.ToInt32(bitPattern, 2);
            }
            catch (Exception)
            {
                continue;
            }

            int convertedAlt = Gillham(g);

            if (convertedAlt == -1)
            {
                continue;
            }

            int error = (g == 2) ? 25 : 50;

            int lower = convertedAlt - error;
            int upper = convertedAlt + error;

            code.Add((lower, upper, $"0b{bitPattern}"));
        }

        code.Sort((x, y) => x.lower.CompareTo(y.lower));

        return code;
    }
}
