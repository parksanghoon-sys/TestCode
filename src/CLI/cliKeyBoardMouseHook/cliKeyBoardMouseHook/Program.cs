using KeyBoardMouseHook;
using System.Runtime.InteropServices;

class Program
{
   

    static void Main(string[] args)
    {
        KeyboardHook.KeyDown += KeyboardHook_KeyDown;
        KeyboardHook.KeyUp += KeyboardHook_KeyUp;
        MouseHook.MouseDown += MouseHook_MouseDown;
        MouseHook.MouseUp += MouseHook_MouseUp;
        MouseHook.MouseMove += MouseHook_MouseMove;
        MouseHook.MouseScroll += MouseHook_MouseScroll;
        KeyboardHook.HookStart();
        if (!MouseHook.HookStart())
        {
            AppendText($"Fail");
        }
        Console.ReadLine();

    }

    private static bool MouseHook_MouseMove(MouseEventType type, int x, int y)
    {
        throw new NotImplementedException();
    }

    private static void AppendText(string text)
    {
        Console.WriteLine(text);
    }
    private static bool MouseHook_MouseScroll(MouseScrollType type)
    {
        AppendText($"MOUSESCROLL: {type}");
        return true;
    }

    private static bool MouseHook_MouseUp(MouseEventType type, int x, int y)
    {
        AppendText($"MOUSEUP: {type} at ({x}, {y})");
        return true;
    }

    private static bool MouseHook_MouseDown(MouseEventType type, int x, int y)
    {
        AppendText($"MOUSEDOWN: {type} at ({x}, {y})");
        return true;
    }

    private static bool KeyboardHook_KeyUp(int vkCode)
    {
        AppendText($"KEYUP : {vkCode}");
        return true;
    }

    private static bool KeyboardHook_KeyDown(int vkCode)
    {
        AppendText($"KEYDOWN : {vkCode}");
        return true;
    }
}
