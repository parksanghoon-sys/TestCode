typedef void(__stdcall* Win32Callback)(int value1, const wchar_t* text1);

extern "C"
{
    __declspec(dllexport)  int __stdcall fnWin32Project1(Win32Callback callback, int value);
};