#include "stdafx.h"

extern CRITICAL_SECTION g_csInstance;

BOOL APIENTRY DllMain(HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        InitializeCriticalSection(&g_csInstance);
        break;
    case DLL_PROCESS_DETACH:
        DeleteCriticalSection(&g_csInstance);
        break;
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
        break;
    }

    return TRUE;
}
