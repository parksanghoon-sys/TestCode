internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        string extensionToRemove = ".pdb";
        //string filePath = @"D:\WPF_Test_UI\src\CLI\cliConsoleArgTest\bin\Debug\net6.0";
        //string[] allFiles = Directory.GetFiles(filePath, "*.*", SearchOption.AllDirectories);


        //foreach (string file in allFiles)
        //{
        //    if(Path.GetExtension(file).Equals(extensionToRemove, StringComparison.OrdinalIgnoreCase))
        //    {
        //        File.Delete(file);
        //    }
        //}
        foreach (string arg in args)
        {
            string[] allFiles = Directory.GetFiles(arg, "*.*", SearchOption.AllDirectories);
            foreach (string file in allFiles)
            {
                if (Path.GetExtension(file).Equals(extensionToRemove, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(file);
                }
            }
        }
        //Console.ReadKey();
    }
}