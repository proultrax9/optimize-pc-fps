using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FpsGodPc.Services;

internal static class HeavenAutomation
{
    private const int SwRestore = 9;
    private const int VkF9 = 0x78;
    private const byte VkSpace = 0x20;
    private const byte VkReturn = 0x0D;
    private const uint KeyeventfKeyup = 0x0002;
    private const int SmCxScreen = 0;
    private const int SmCyScreen = 1;
    private const uint MouseeventfLeftDown = 0x0002;
    private const uint MouseeventfLeftUp = 0x0004;

    public static async Task<bool> WaitForMainWindowAsync(Process process, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            process.Refresh();
            if (process.HasExited)
            {
                return false;
            }

            if (process.MainWindowHandle != IntPtr.Zero || FindWindowForProcess(process.Id) != IntPtr.Zero)
            {
                return true;
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }

        return FindWindowForProcess(process.Id) != IntPtr.Zero;
    }

    public static void FocusWindow(Process process)
    {
        var hwnd = process.MainWindowHandle != IntPtr.Zero
            ? process.MainWindowHandle
            : FindWindowForProcess(process.Id);
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        ShowWindow(hwnd, SwRestore);
        SetForegroundWindow(hwnd);
    }

    public static void FocusAndStartBenchmark(Process process)
    {
        FocusWindow(process);
        Thread.Sleep(400);
        SendF9();
    }

    /// <summary>
    /// Best-effort "start the sequence" nudge for Unity demos that wait for input on a
    /// title/menu before the heavy scene plays. Focuses the window and taps Space + Enter.
    /// Harmless if the demo auto-plays.
    /// </summary>
    public static void FocusAndNudgeStart(Process process)
    {
        FocusWindow(process);
        Thread.Sleep(500);
        SendKey(VkSpace);
        Thread.Sleep(250);
        SendKey(VkReturn);
    }

    public static void SendKey(byte virtualKey)
    {
        keybd_event(virtualKey, 0, 0, UIntPtr.Zero);
        keybd_event(virtualKey, 0, KeyeventfKeyup, UIntPtr.Zero);
    }

    /// <summary>Primary screen size in pixels.</summary>
    public static (int Width, int Height) GetPrimaryScreenSize() =>
        (GetSystemMetrics(SmCxScreen), GetSystemMetrics(SmCyScreen));

    /// <summary>Moves the cursor to an absolute screen position and performs a left click.</summary>
    public static void ClickAt(int x, int y)
    {
        SetCursorPos(x, y);
        Thread.Sleep(120);
        mouse_event(MouseeventfLeftDown, 0, 0, 0, UIntPtr.Zero);
        Thread.Sleep(50);
        mouse_event(MouseeventfLeftUp, 0, 0, 0, UIntPtr.Zero);
    }

    public static void KillProcessTree(Process? process)
    {
        if (process is null)
        {
            return;
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(10_000);
            }
        }
        catch
        {
            // Best effort.
        }
    }

    private static void SendF9()
    {
        keybd_event((byte)VkF9, 0, 0, UIntPtr.Zero);
        keybd_event((byte)VkF9, 0, KeyeventfKeyup, UIntPtr.Zero);
    }

    private static IntPtr FindWindowForProcess(int processId)
    {
        IntPtr found = IntPtr.Zero;
        EnumWindows((hwnd, _) =>
        {
            GetWindowThreadProcessId(hwnd, out int pid);
            if (pid != processId || !IsWindowVisible(hwnd))
            {
                return true;
            }

            found = hwnd;
            return false;
        }, IntPtr.Zero);
        return found;
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
}
