using System.ComponentModel;
using System.Diagnostics;

namespace FpsGodPc.Services;

/// <summary>
/// Sets and restores Windows process priority classes for game executables.
/// Operations are best-effort: access-denied errors are caught and reported, not thrown.
/// </summary>
public sealed class ProcessPriorityService
{
    /// <summary>
    /// Raises the priority of every currently-running process whose name matches
    /// <paramref name="executablePath"/> to <paramref name="targetClass"/>.
    /// Returns the number of processes successfully raised.
    /// </summary>
    public (int Raised, int Denied, string Message) RaiseByExecutable(
        string executablePath,
        ProcessPriorityClass targetClass = ProcessPriorityClass.High)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return (0, 0, "No executable specified.");
        }

        var processName = Path.GetFileNameWithoutExtension(executablePath);
        var raised = 0;
        var denied = 0;

        foreach (var proc in Process.GetProcessesByName(processName))
        {
            using (proc)
            {
                try
                {
                    proc.PriorityClass = targetClass;
                    raised++;
                }
                catch (Win32Exception)
                {
                    denied++;
                }
                catch (InvalidOperationException)
                {
                    // Process exited between enumeration and set — ignore.
                }
            }
        }

        if (raised == 0 && denied == 0)
        {
            return (0, 0, $"No running processes found matching '{processName}'.");
        }

        var label = PriorityLabel(targetClass);
        var msg = denied > 0
            ? $"Priority set to {label} on {raised} process(es); {denied} denied (requires Administrator or same-session elevation)."
            : $"Priority set to {label} on {raised} process(es) matching '{processName}'.";

        return (raised, denied, msg);
    }

    /// <summary>
    /// Restores the priority of every running process matching <paramref name="executablePath"/>
    /// back to <see cref="ProcessPriorityClass.Normal"/>.
    /// </summary>
    public (int Restored, string Message) RestoreByExecutable(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return (0, "No executable specified.");
        }

        var processName = Path.GetFileNameWithoutExtension(executablePath);
        var restored = 0;

        foreach (var proc in Process.GetProcessesByName(processName))
        {
            using (proc)
            {
                try
                {
                    proc.PriorityClass = ProcessPriorityClass.Normal;
                    restored++;
                }
                catch (Win32Exception) { /* access denied — ignore */ }
                catch (InvalidOperationException) { /* process gone — ignore */ }
            }
        }

        return restored == 0
            ? (0, $"No running processes found matching '{processName}' to restore.")
            : (restored, $"Priority restored to Normal on {restored} process(es) matching '{processName}'.");
    }

    /// <summary>
    /// Sets priority on a specific <see cref="Process"/> instance.
    /// Returns (true, message) on success, (false, message) on access-denied or other error.
    /// </summary>
    public (bool Success, string Message) SetPriority(
        Process process,
        ProcessPriorityClass targetClass = ProcessPriorityClass.High)
    {
        try
        {
            process.PriorityClass = targetClass;
            return (true, $"Process '{process.ProcessName}' priority set to {PriorityLabel(targetClass)}.");
        }
        catch (Win32Exception ex)
        {
            return (false, $"Cannot set priority on '{process.ProcessName}': {ex.Message} (requires Administrator or matching session).");
        }
        catch (InvalidOperationException ex)
        {
            return (false, $"Process '{process.ProcessName}' is no longer running: {ex.Message}");
        }
    }

    private static string PriorityLabel(ProcessPriorityClass cls) => cls switch
    {
        ProcessPriorityClass.RealTime => "Real-Time",
        ProcessPriorityClass.High => "High",
        ProcessPriorityClass.AboveNormal => "Above Normal",
        ProcessPriorityClass.Normal => "Normal",
        ProcessPriorityClass.BelowNormal => "Below Normal",
        ProcessPriorityClass.Idle => "Idle",
        _ => cls.ToString(),
    };
}
