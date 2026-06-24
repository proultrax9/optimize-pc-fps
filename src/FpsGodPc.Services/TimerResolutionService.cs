using System.Runtime.InteropServices;

namespace FpsGodPc.Services;

/// <summary>
/// Manages Windows timer resolution via ntdll NtSetTimerResolution.
/// Resolution units are 100-nanosecond intervals (e.g. 5000 = 0.5 ms).
/// The resolution is held per-process: Enable() holds it for the process lifetime
/// until Disable() is called. Thread-safe; safe to call Enable/Disable multiple times.
/// </summary>
public sealed class TimerResolutionService : IDisposable
{
    // 100ns units: 5000 = 0.5 ms, 10000 = 1 ms, 15600 = Windows default ~15.6 ms
    private const uint DesiredResolution100Ns = 5000;

    [DllImport("ntdll.dll", SetLastError = false)]
    private static extern int NtSetTimerResolution(uint DesiredResolution, bool SetResolution, out uint CurrentResolution);

    [DllImport("ntdll.dll", SetLastError = false)]
    private static extern int NtQueryTimerResolution(out uint MinimumResolution, out uint MaximumResolution, out uint CurrentResolution);

    private readonly object _lock = new();
    private bool _enabled;
    private uint _achievedResolution100Ns;

    /// <summary>
    /// Requests the minimum achievable timer resolution (targeting 0.5 ms / 5000 units).
    /// Returns a human-readable string describing the achieved resolution.
    /// Never throws — returns an error description on failure.
    /// </summary>
    public string Enable()
    {
        lock (_lock)
        {
            // Query what the system supports first
            int queryStatus = NtQueryTimerResolution(
                out uint minRes,   // minimum (coarsest) resolution the system supports
                out uint maxRes,   // maximum (finest) resolution the system supports
                out uint curRes);

            // Request the finest resolution the hardware supports, or our target, whichever is coarser
            uint desired = (queryStatus == 0 && maxRes > 0)
                ? Math.Max(maxRes, DesiredResolution100Ns)   // can't go finer than hardware max
                : DesiredResolution100Ns;

            int status = NtSetTimerResolution(desired, true, out uint current);

            if (status == 0 || status == 1)   // STATUS_SUCCESS or STATUS_TIMER_RESOLUTION_NOT_CHANGED
            {
                _enabled = true;
                _achievedResolution100Ns = current > 0 ? current : desired;
                double ms = _achievedResolution100Ns / 10_000.0;
                return $"Timer resolution set to {ms:F2} ms ({_achievedResolution100Ns} × 100 ns units).";
            }

            // Non-fatal failure path
            _enabled = false;
            return $"Timer resolution request failed (NTSTATUS 0x{status:X8}). Running without elevated timer resolution.";
        }
    }

    /// <summary>
    /// Releases the timer resolution request so the OS can revert to default scheduling.
    /// </summary>
    public string Disable()
    {
        lock (_lock)
        {
            if (!_enabled)
            {
                return "Timer resolution was not active — nothing to release.";
            }

            int status = NtSetTimerResolution(DesiredResolution100Ns, false, out _);
            _enabled = false;
            _achievedResolution100Ns = 0;

            return status == 0 || status == 1
                ? "Timer resolution released; system reverted to default scheduling interval."
                : $"Timer resolution release returned NTSTATUS 0x{status:X8} (may still be reverted on process exit).";
        }
    }

    /// <summary>True if Enable() has been called and succeeded without a subsequent Disable().</summary>
    public bool IsEnabled
    {
        get { lock (_lock) { return _enabled; } }
    }

    /// <summary>The achieved resolution in 100 ns units after Enable(), or 0 if not active.</summary>
    public uint AchievedResolution100Ns
    {
        get { lock (_lock) { return _achievedResolution100Ns; } }
    }

    /// <summary>The achieved resolution in milliseconds after Enable(), or 0 if not active.</summary>
    public double AchievedResolutionMs
    {
        get { lock (_lock) { return _achievedResolution100Ns > 0 ? _achievedResolution100Ns / 10_000.0 : 0; } }
    }

    public void Dispose()
    {
        // Disable() is locked and idempotent (no-ops if not enabled), so call it
        // unconditionally rather than racing on an unlocked _enabled read.
        try { Disable(); } catch { /* best-effort on dispose */ }
    }
}
