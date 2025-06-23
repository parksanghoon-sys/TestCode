// dllmain.cpp : DLL 애플리케이션의 진입점을 정의합니다.
#include "pch.h"
#include <Windows.h>
#include <wincred.h>


extern "C" __declspec(dllexport)
BOOL ValidateUser(const wchar_t* username, const wchar_t* password, const wchar_t* domain)
{
    HANDLE hToken;
    BOOL result = LogonUserW(
        username,
        domain,
        password,
        LOGON32_LOGON_INTERACTIVE,
        LOGON32_PROVIDER_DEFAULT,
        &hToken);

    if (result) {
        CloseHandle(hToken);
    }
    return result;
}
extern "C" __declspec(dllexport)
BOOL ReadCredential(const wchar_t* target, wchar_t* usernameOut, DWORD usernameSize, wchar_t* passwordOut, DWORD passwordSize)
{
    PCREDENTIALW pCred = nullptr;
    if (!CredReadW(target, CRED_TYPE_GENERIC, 0, &pCred)) {
        return FALSE;
    }

    if (pCred->UserName && usernameOut)
        wcsncpy_s(usernameOut, usernameSize, pCred->UserName, _TRUNCATE);

    if (pCred->CredentialBlob && passwordOut)
        wcsncpy_s(passwordOut, passwordSize, (wchar_t*)pCred->CredentialBlob, _TRUNCATE);

    CredFree(pCred);
    return TRUE;
}
int num = 100;
extern "C" __declspec(dllexport) 
int GetNumber()
{
    return num;
}

