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

public sealed class TelemetryService
{
    private readonly ProcessRunner _runner;
    private readonly HardwareInfo _hardware;
    private bool _hardwareReady;

    public TelemetryService(ProcessRunner runner)
    {
        _runner = runner;
        _hardware = new HardwareInfo();
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
    }

    public TelemetrySnapshot Collect()
    {
        var snapshot = new TelemetrySnapshot();

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
