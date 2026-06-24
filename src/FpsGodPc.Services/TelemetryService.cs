using Hardware.Info;
using System.Diagnostics;
using System.Management;

namespace FpsGodPc.Services;

public sealed class TelemetrySnapshot
{
    public string CpuName { get; set; } = "—";
    public float CpuUsagePct { get; set; }
    public float? CpuTempC { get; set; }
    public string GpuName { get; set; } = "—";
    public float? GpuUsagePct { get; set; }
    public float? GpuTempC { get; set; }
    public float MemoryUsedGb { get; set; }
    public float MemoryTotalGb { get; set; }
    public string PowerPlan { get; set; } = "—";
    public int ProcessCount { get; set; }
}

/// <summary>
/// Collects a telemetry snapshot on each Collect() call.
///
/// CONSTRUCTOR PARAMETERS (for DI integrator):
///   - ProcessRunner runner      — existing dependency, registered as singleton
///   - HardwareMonitorService hardwareMonitor — NEW singleton; register with:
///         services.AddSingleton&lt;HardwareMonitorService&gt;();
///     before the TelemetryService registration. HardwareMonitorService is IDisposable
///     so the DI container will dispose it correctly when the app exits.
/// </summary>
public sealed class TelemetryService
{
    private readonly ProcessRunner _runner;
    private readonly HardwareMonitorService _hardwareMonitor;
    private readonly HardwareInfo _hardware;

    // Lazy-init guard: Hardware.Info WMI calls are deferred to first Collect().
    // volatile: Collect() runs on thread-pool threads (Dashboard Task.Run), so these
    // flags must be visible across threads without relying on the init lock at read time.
    private volatile bool _hardwareInitialized;
    private volatile bool _hardwareReady;
    private readonly object _initLock = new();

    public TelemetryService(ProcessRunner runner, HardwareMonitorService hardwareMonitor)
    {
        _runner = runner;
        _hardwareMonitor = hardwareMonitor;
        // Only allocate the HardwareInfo object here — do NOT call any Refresh*
        // methods in the constructor. Those are heavy WMI calls deferred to first use.
        _hardware = new HardwareInfo();
    }

    public TelemetrySnapshot Collect()
    {
        var snapshot = new TelemetrySnapshot();

        // ------------------------------------------------------------------
        // Hardware.Info: lazy one-time init + per-call refresh
        // ------------------------------------------------------------------
        EnsureHardwareInitialized();

        try
        {
            if (_hardwareReady)
            {
                _hardware.RefreshCPUList();
                _hardware.RefreshVideoControllerList();
                _hardware.RefreshMemoryStatus();

                var cpu = _hardware.CpuList.FirstOrDefault();
                if (cpu is not null)
                {
                    snapshot.CpuName = string.IsNullOrWhiteSpace(cpu.Name) ? "CPU" : cpu.Name.Trim();
                    snapshot.CpuUsagePct = (float)cpu.PercentProcessorTime;
                }

                var gpu = _hardware.VideoControllerList.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v.Name));
                if (gpu is not null)
                {
                    snapshot.GpuName = gpu.Name.Trim();
                }

                snapshot.MemoryTotalGb = (float)(_hardware.MemoryStatus.TotalPhysical / 1024.0 / 1024.0 / 1024.0);
                snapshot.MemoryUsedGb = snapshot.MemoryTotalGb - (float)(_hardware.MemoryStatus.AvailablePhysical / 1024.0 / 1024.0 / 1024.0);
            }
        }
        catch
        {
            // WMI/Hardware.Info may fail on some systems.
        }

        // ------------------------------------------------------------------
        // CPU usage via WMI (more reliable than Hardware.Info PercentProcessorTime)
        // ------------------------------------------------------------------
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT LoadPercentage FROM Win32_Processor");
            foreach (var obj in searcher.Get().Cast<ManagementObject>())
            {
                if (obj["LoadPercentage"] is uint load)
                {
                    snapshot.CpuUsagePct = load;
                    break;
                }
            }
        }
        catch
        {
            // ignore
        }

        // ------------------------------------------------------------------
        // Real GPU load + CPU/GPU temperatures via LibreHardwareMonitor
        // ------------------------------------------------------------------
        try
        {
            var readings = _hardwareMonitor.Read();

            snapshot.CpuTempC = readings.CpuTempC;
            snapshot.GpuTempC = readings.GpuTempC;
            snapshot.GpuUsagePct = readings.GpuLoadPct;

            // If LibreHardwareMonitor detected a GPU name and Hardware.Info did not,
            // use the LibreHardwareMonitor name as a fallback.
            if (!string.IsNullOrWhiteSpace(readings.GpuName)
                && (snapshot.GpuName == "—" || string.IsNullOrWhiteSpace(snapshot.GpuName)))
            {
                snapshot.GpuName = readings.GpuName!;
            }
        }
        catch
        {
            // LibreHardwareMonitor is always defensive internally, but guard here too.
        }

        // ------------------------------------------------------------------
        // Power plan + process count
        // ------------------------------------------------------------------
        try
        {
            snapshot.PowerPlan = ReadPowerPlan();
        }
        catch
        {
            snapshot.PowerPlan = "Unknown";
        }

        snapshot.ProcessCount = Process.GetProcesses().Length;
        return snapshot;
    }

    // ------------------------------------------------------------------
    // Private helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// Performs the initial Hardware.Info WMI enumeration once, lazily, on first
    /// Collect() call rather than in the constructor to keep app startup fast.
    /// </summary>
    private void EnsureHardwareInitialized()
    {
        if (_hardwareInitialized)
        {
            return;
        }

        lock (_initLock)
        {
            if (_hardwareInitialized)
            {
                return;
            }

            try
            {
                _hardware.RefreshCPUList();
                _hardware.RefreshVideoControllerList();
                _hardware.RefreshMemoryStatus();
                _hardwareReady = true;
            }
            catch
            {
                _hardwareReady = false;
            }
            finally
            {
                _hardwareInitialized = true;
            }
        }
    }

    private string ReadPowerPlan()
    {
        try
        {
            return _runner.RunCommand("powercfg", "/getactivescheme");
        }
        catch
        {
            return _runner.RunPowerShell(
                "(Get-CimInstance -Namespace root/cimv2/power -ClassName Win32_PowerPlan -ErrorAction SilentlyContinue | Where-Object {$_.IsActive}).ElementName");
        }
    }
}
