#include "pch.h"
#include "Win32Project1.h"

__declspec(dllexport) int __stdcall fnWin32Project1(Win32Callback callback, int value)
{
    callback(value, L"test is good");
    return 42;
}