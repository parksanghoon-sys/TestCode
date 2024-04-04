using System.Runtime.InteropServices;

internal class Program
{
    public delegate void Win32Callback(int value1, [MarshalAs(UnmanagedType.LPWStr)] string text1);

    [DllImport("Win32Project1.dll")]
    static extern int fnWin32Project1(Win32Callback callback, int value);
    private static void Main(string[] args)
    {
        s_callback = MyCallbackFunc;

        var test = fnWin32Project1(s_callback, 5);
        Console.WriteLine(test);
        Console.ReadLine();
    }
    public static Win32Callback s_callback;

    public static void MyCallbackFunc(int value1, string text1)
    {
        Console.WriteLine(value1 + ":" + text1);
    }
}