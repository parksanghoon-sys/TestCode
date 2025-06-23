using System.Text;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        string[] extensionToRemoves = { ".pdb" };
        //string filePath = @"D:\WPF_Test_UI\src\CLI\cliConsoleArgTest\bin\Debug\net6.0";
        //string[] allFiles = Directory.GetFiles(filePath, "*.*", SearchOption.AllDirectories);


        //foreach (string file in allFiles)
        //{
        //    if(Path.GetExtension(file).Equals(extensionToRemove, StringComparison.OrdinalIgnoreCase))
        //    {
        //        File.Delete(file);
        //    }
        //}
        //string data = "311.000";
        //double freqeuncyVaild = 0.0;
        //double _oldFrequency = 0d;
        //data = data.Trim().PadRight(7, '0');
        //var stringTemp = data.Remove(0, 1);
        //double.TryParse(stringTemp, out freqeuncyVaild);

        //if (freqeuncyVaild < 0.0) _oldFrequency = 0;
        //else if (freqeuncyVaild > 99.975) _oldFrequency = 99.975;
        //else _oldFrequency = freqeuncyVaild;

        //var str = string.Format("A{0:00.000}", _oldFrequency);
        //StringBuilder stringBuilder = new StringBuilder();
        //double frequency = 318.000;
        //var strFreqeuncy = frequency.ToString();

        //strFreqeuncy = strFreqeuncy.Remove(0, 1);
        //var test = "A" + strFreqeuncy;
        //var test2 = string.Format("A{0:00.000}", test);



        foreach (string arg in args)
        {
            string[] allFiles = Directory.GetFiles(arg, "*.*", SearchOption.AllDirectories);
            foreach (string file in allFiles)
            {
                foreach (string extension in extensionToRemoves)
                {
                    if (Path.GetExtension(file).Equals(extension, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(file);
                    }
                }
               
            }
        }
        //Console.ReadKey();
    }
}