using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FpsGodPc.Services;

public static class StandbyMemoryService
{
    private const int SystemMemoryListInformation = 80;
    private const int MemoryEmptyStandbyList = 4;

    [DllImport("psapi.dll", SetLastError = true)]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);

    [DllImport("ntdll.dll")]
    private static extern int NtSetSystemInformation(int infoClass, ref int info, int length);

    public static string FlushStandbyMemory()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        EmptyWorkingSet(Process.GetCurrentProcess().Handle);

        var command = MemoryEmptyStandbyList;
        var status = NtSetSystemInformation(SystemMemoryListInformation, ref command, sizeof(int));
        return status switch
        {
            0 => "Standby memory list flushed.",
            unchecked((int)0xC0000022) =>
                "Working set trimmed. Run as Administrator to flush the full standby list.",
            _ => "Memory trim requested (best effort).",
        };
    }
}
