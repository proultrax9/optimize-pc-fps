using System.Management;
using Hardware.Info;

namespace FpsGodPc.Services;

public sealed class SystemInfoSnapshot
{
    public string Username { get; set; } = "User";
    public uint PerformanceScore { get; set; }
    public string CpuName { get; set; } = "—";
    public int CpuCores { get; set; }
    public int CpuPhysicalCores { get; set; }
    public string GpuName { get; set; } = "—";
    public string GpuVramGb { get; set; } = "—";
    public string MemoryTotalGb { get; set; } = "—";
    public string MemoryType { get; set; } = "—";
    public string OsName { get; set; } = "—";
    public string OsVersion { get; set; } = "—";
    public string StorageName { get; set; } = "—";
    public string StorageTotalGb { get; set; } = "—";
    public uint TweaksTotal { get; set; }
    public uint TweaksActive { get; set; }
    public string PowerPlan { get; set; } = "—";
    public bool GameModeEnabled { get; set; }
    public string BiosManufacturer { get; set; } = "—";
    public string BiosVersion { get; set; } = "—";
    public string BiosSerial { get; set; } = "—";
    public string BiosReleaseDate { get; set; } = "—";
    public string DramSpeedMhz { get; set; } = "—";
    public string ProcessorSpeedMhz { get; set; } = "—";
}

public sealed class SystemInfoService
{
    private readonly ProcessRunner _runner;
    private readonly AppStateStore _store;
    private readonly HardwareInfo _hardware;

    public SystemInfoService(ProcessRunner runner, AppStateStore store)
    {
        _runner = runner;
        _store = store;
        _hardware = new HardwareInfo();
        try { _hardware.RefreshAll(); } catch { }
    }

    public SystemInfoSnapshot Collect(uint tweaksTotal)
    {
        RefreshHardware();

        var cpu = _hardware.CpuList.FirstOrDefault();
        var gpu = _hardware.VideoControllerList.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v.Name));
        var disk = _hardware.DriveList.FirstOrDefault();
        var memoryGb = _hardware.MemoryStatus.TotalPhysical / 1024.0 / 1024.0 / 1024.0;
        var powerPlan = ReadPowerPlan();
        var gameMode = ReadGameMode();
        var bios = _hardware.BiosList.FirstOrDefault();

        var info = new SystemInfoSnapshot
        {
            Username = Environment.UserName,
            CpuName = OrDash(cpu?.Name),
            CpuCores = cpu?.NumberOfLogicalProcessors > 0 ? (int)cpu.NumberOfLogicalProcessors : 1,
            CpuPhysicalCores = cpu?.NumberOfCores > 0 ? (int)cpu.NumberOfCores : 1,
            GpuName = OrDash(gpu?.Name),
            GpuVramGb = gpu?.AdapterRAM > 0 ? $"{gpu.AdapterRAM / 1024.0 / 1024.0 / 1024.0:F1}" : "—",
            MemoryTotalGb = memoryGb > 0 ? $"{memoryGb:F2}" : "—",
            MemoryType = "DDR",
            OsName = Environment.OSVersion.Platform.ToString(),
            OsVersion = Environment.OSVersion.VersionString,
            StorageName = OrDash(disk?.Model),
            StorageTotalGb = disk?.Size > 0 ? $"{disk.Size / 1024.0 / 1024.0 / 1024.0:F1}" : "—",
            TweaksTotal = tweaksTotal,
            TweaksActive = (uint)_store.AppliedIds().Count,
            PowerPlan = OrDash(powerPlan),
            GameModeEnabled = gameMode,
            BiosManufacturer = OrDash(bios?.Manufacturer),
            BiosVersion = OrDash(bios?.Version),
            BiosSerial = OrDash(bios?.SerialNumber),
            BiosReleaseDate = OrDash(bios?.ReleaseDate?.ToString()),
            ProcessorSpeedMhz = cpu?.CurrentClockSpeed > 0 ? cpu.CurrentClockSpeed.ToString() : "—",
            PerformanceScore = ComputeScore(powerPlan, gameMode, memoryGb),
        };

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Caption, Version FROM Win32_OperatingSystem");
            foreach (var obj in searcher.Get().Cast<ManagementObject>())
            {
                info.OsName = obj["Caption"]?.ToString() ?? info.OsName;
                info.OsVersion = obj["Version"]?.ToString() ?? info.OsVersion;
                break;
            }
        }
        catch { }

        return info;
    }

    private void RefreshHardware()
    {
        try
        {
            _hardware.RefreshCPUList();
            _hardware.RefreshVideoControllerList();
            _hardware.RefreshDriveList();
            _hardware.RefreshBIOSList();
            _hardware.RefreshMemoryStatus();
        }
        catch { }
    }

    private string ReadPowerPlan()
    {
        try
        {
            var raw = _runner.RunCommand("powercfg", "/getactivescheme");
            var start = raw.LastIndexOf('(');
            var end = raw.LastIndexOf(')');
            if (start >= 0 && end > start)
            {
                return raw[(start + 1)..end].Trim();
            }

            return raw;
        }
        catch
        {
            return "Unknown";
        }
    }

    private bool ReadGameMode()
    {
        try
        {
            var raw = _runner.RunPowerShell(
                "(Get-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\GameBar' -Name 'AutoGameModeEnabled' -ErrorAction SilentlyContinue).AutoGameModeEnabled");
            return raw.Trim() == "1";
        }
        catch
        {
            return false;
        }
    }

    private static string OrDash(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();

    private static uint ComputeScore(string powerPlan, bool gameMode, double memoryGb)
    {
        var score = 55;
        var plan = powerPlan.ToLowerInvariant();
        if (plan.Contains("ultimate") || plan.Contains("high performance")) score += 20;
        else if (plan.Contains("balanced")) score += 8;
        if (gameMode) score += 10;
        if (memoryGb >= 32) score += 12;
        else if (memoryGb >= 16) score += 8;
        else if (memoryGb >= 8) score += 4;
        return (uint)Math.Clamp(score, 0, 100);
    }
}
