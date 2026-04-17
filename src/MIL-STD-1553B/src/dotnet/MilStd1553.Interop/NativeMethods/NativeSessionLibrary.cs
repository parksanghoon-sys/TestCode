using System.Runtime.InteropServices;
using System.Text;
using MilStd1553.Interop.Exceptions;

namespace MilStd1553.Interop.NativeMethods;

/// <summary>
/// 네이티브 세션 C ABI 엔트리 포인트를 명시적으로 적재해 호출합니다.
/// </summary>
internal sealed class NativeSessionLibrary
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private delegate int OpenSessionDelegate(
        string scenarioJson,
        StringBuilder sessionHandleBuffer,
        int sessionHandleCapacity,
        out int requiredSessionHandleCapacity,
        StringBuilder healthJsonBuffer,
        int healthJsonCapacity,
        out int requiredHealthJsonCapacity);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private delegate int StopSessionDelegate(string sessionHandle);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private delegate int SwitchBusDelegate(string sessionHandle, int busLine);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private delegate int GetHealthSnapshotDelegate(
        string sessionHandle,
        StringBuilder healthJsonBuffer,
        int healthJsonCapacity,
        out int requiredHealthJsonCapacity);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private delegate int PollTelemetryDelegate(
        string sessionHandle,
        StringBuilder telemetryJsonBuffer,
        int telemetryJsonCapacity,
        out int requiredTelemetryJsonCapacity);

    private readonly OpenSessionDelegate openSession;
    private readonly StopSessionDelegate stopSession;
    private readonly SwitchBusDelegate switchBus;
    private readonly GetHealthSnapshotDelegate getHealthSnapshot;
    private readonly PollTelemetryDelegate pollTelemetry;

    private NativeSessionLibrary(
        OpenSessionDelegate openSession,
        StopSessionDelegate stopSession,
        SwitchBusDelegate switchBus,
        GetHealthSnapshotDelegate getHealthSnapshot,
        PollTelemetryDelegate pollTelemetry)
    {
        this.openSession = openSession;
        this.stopSession = stopSession;
        this.switchBus = switchBus;
        this.getHealthSnapshot = getHealthSnapshot;
        this.pollTelemetry = pollTelemetry;
    }

    /// <summary>
    /// 기본 런타임 경로에서 네이티브 세션 라이브러리를 적재합니다.
    /// </summary>
    /// <returns>적재된 네이티브 세션 라이브러리입니다.</returns>
    internal static NativeSessionLibrary LoadDefault()
    {
        var libraryPath = NativeLibraryPathResolver.GetExpectedLibraryPath();
        nint libraryHandle;

        try
        {
            libraryHandle = NativeLibrary.Load(libraryPath);
        }
        catch (DllNotFoundException exception)
        {
            throw CreateLoadException(
                NativeLibraryFailureKind.LibraryNotFound,
                "LoadLibrary",
                libraryPath,
                "네이티브 런타임 DLL을 찾지 못했습니다.",
                exception);
        }
        catch (BadImageFormatException exception)
        {
            throw CreateLoadException(
                NativeLibraryFailureKind.InvalidBinary,
                "LoadLibrary",
                libraryPath,
                "네이티브 런타임 DLL을 적재할 수 없습니다. 파일 형식이나 아키텍처를 확인하세요.",
                exception);
        }

        try
        {
            return new NativeSessionLibrary(
                GetExport<OpenSessionDelegate>(libraryHandle, "MilStd1553_OpenSession", "OpenSession", libraryPath),
                GetExport<StopSessionDelegate>(libraryHandle, "MilStd1553_StopSession", "StopSession", libraryPath),
                GetExport<SwitchBusDelegate>(libraryHandle, "MilStd1553_SwitchBus", "SwitchBus", libraryPath),
                GetExport<GetHealthSnapshotDelegate>(libraryHandle, "MilStd1553_GetHealthSnapshot", "GetHealthSnapshot", libraryPath),
                GetExport<PollTelemetryDelegate>(libraryHandle, "MilStd1553_PollTelemetry", "PollTelemetry", libraryPath));
        }
        catch
        {
            NativeLibrary.Free(libraryHandle);
            throw;
        }
    }

    /// <summary>
    /// `OpenSession` 네이티브 함수를 호출합니다.
    /// </summary>
    internal int OpenSession(
        string scenarioJson,
        StringBuilder sessionHandleBuffer,
        int sessionHandleCapacity,
        out int requiredSessionHandleCapacity,
        StringBuilder healthJsonBuffer,
        int healthJsonCapacity,
        out int requiredHealthJsonCapacity)
    {
        return openSession(
            scenarioJson,
            sessionHandleBuffer,
            sessionHandleCapacity,
            out requiredSessionHandleCapacity,
            healthJsonBuffer,
            healthJsonCapacity,
            out requiredHealthJsonCapacity);
    }

    /// <summary>
    /// `StopSession` 네이티브 함수를 호출합니다.
    /// </summary>
    internal int StopSession(string sessionHandle)
    {
        return stopSession(sessionHandle);
    }

    /// <summary>
    /// `SwitchBus` 네이티브 함수를 호출합니다.
    /// </summary>
    internal int SwitchBus(string sessionHandle, int busLine)
    {
        return switchBus(sessionHandle, busLine);
    }

    /// <summary>
    /// `GetHealthSnapshot` 네이티브 함수를 호출합니다.
    /// </summary>
    internal int GetHealthSnapshot(
        string sessionHandle,
        StringBuilder healthJsonBuffer,
        int healthJsonCapacity,
        out int requiredHealthJsonCapacity)
    {
        return getHealthSnapshot(
            sessionHandle,
            healthJsonBuffer,
            healthJsonCapacity,
            out requiredHealthJsonCapacity);
    }

    /// <summary>
    /// `PollTelemetry` 네이티브 함수를 호출합니다.
    /// </summary>
    internal int PollTelemetry(
        string sessionHandle,
        StringBuilder telemetryJsonBuffer,
        int telemetryJsonCapacity,
        out int requiredTelemetryJsonCapacity)
    {
        return pollTelemetry(
            sessionHandle,
            telemetryJsonBuffer,
            telemetryJsonCapacity,
            out requiredTelemetryJsonCapacity);
    }

    private static TDelegate GetExport<TDelegate>(
        nint libraryHandle,
        string exportName,
        string operationName,
        string libraryPath)
        where TDelegate : Delegate
    {
        try
        {
            var exportHandle = NativeLibrary.GetExport(libraryHandle, exportName);
            return Marshal.GetDelegateForFunctionPointer<TDelegate>(exportHandle);
        }
        catch (EntryPointNotFoundException exception)
        {
            throw CreateLoadException(
                NativeLibraryFailureKind.EntryPointMissing,
                operationName,
                libraryPath,
                $"네이티브 런타임 DLL에서 {operationName} 엔트리 포인트를 찾지 못했습니다.",
                exception);
        }
    }

    private static NativeLibraryLoadException CreateLoadException(
        NativeLibraryFailureKind failureKind,
        string operationName,
        string libraryPath,
        string message,
        Exception innerException)
    {
        return new NativeLibraryLoadException(
            failureKind,
            operationName,
            libraryPath,
            message,
            innerException);
    }
}
