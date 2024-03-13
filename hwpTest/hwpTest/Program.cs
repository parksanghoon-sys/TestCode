using AxHWPCONTROLLib;
using hwpTest;

internal class Program
{
    private static void Main(string[] args)
    {
        string str = args[0];
        string fileName = args[1];
        string methodName = args[2];
        int int32 = Convert.ToInt32(args[3]);
        List<string> excludePaths = Program.SetupExcludePaths(Path.GetDirectoryName(str));
        Console.Write(new CallerFinder.CallerFinder(str, excludePaths).Find(fileName, methodName, int32));
    }

    private static List<string> SetupExcludePaths(string inputRoot)
    {
        List<string> stringList = new List<string>();
        string path1 = Program.CombinePath(inputRoot, ".gendoc_exclude");
        if (File.Exists(path1))
            stringList = ((IEnumerable<string>)File.ReadAllLines(path1)).Where<string>((Func<string, bool>)(path => !string.IsNullOrWhiteSpace(path))).Select<string, string>((Func<string, string>)(path => path.Trim())).ToList<string>();
        return stringList;
    }

    private static string CombinePath(string first, string second) => string.Format("{0}{1}{2}", (object)first.TrimEnd(Path.DirectorySeparatorChar), (object)Path.DirectorySeparatorChar, (object)second.TrimStart(Path.DirectorySeparatorChar));
}