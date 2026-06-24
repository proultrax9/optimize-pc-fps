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

    // Optional hardware-tier sub-scores (0-100 range each, set by ComputeScore).
    // Exposed so the UI can display a breakdown if desired.
    public int CpuScore { get; set; }
    public int GpuScore { get; set; }
    public int RamScore { get; set; }
}

public sealed class SystemInfoService
{
    private readonly ProcessRunner _runner;
    private readonly AppStateStore _store;
    private readonly HardwareInfo _hardware;

    // Lazy-init guard: RefreshAll() is heavy — deferred to first Collect().
    private bool _hardwareInitialized;
    private readonly object _initLock = new();

    public SystemInfoService(ProcessRunner runner, AppStateStore store)
    {
        _runner = runner;
        _store = store;
        // Allocate HardwareInfo but do NOT call RefreshAll() here — it blocks startup.
        _hardware = new HardwareInfo();
    }

    public SystemInfoSnapshot Collect(uint tweaksTotal)
    {
        // Lazy one-time full refresh, then per-call partial refresh.
        EnsureHardwareInitialized();
        RefreshHardware();

        var cpu = _hardware.CpuList.FirstOrDefault();
        var gpu = _hardware.VideoControllerList.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v.Name));
        var disk = _hardware.DriveList.FirstOrDefault();
        var memoryGb = _hardware.MemoryStatus.TotalPhysical / 1024.0 / 1024.0 / 1024.0;
        var powerPlan = ReadPowerPlan();
        var gameMode = ReadGameMode();
        var bios = _hardware.BiosList.FirstOrDefault();

        var (score, cpuScore, gpuScore, ramScore) = ComputeScore(cpu, gpu?.Name, memoryGb, powerPlan, gameMode);

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
            PerformanceScore = score,
            CpuScore = cpuScore,
            GpuScore = gpuScore,
            RamScore = ramScore,
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

    // ------------------------------------------------------------------
    // Hardware refresh helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// Performs the expensive one-time full hardware refresh lazily on first
    /// Collect() call rather than in the constructor.
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
                _hardware.RefreshAll();
            }
            catch { }
            finally
            {
                _hardwareInitialized = true;
            }
        }
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

    // ------------------------------------------------------------------
    // Score computation
    // ------------------------------------------------------------------

    /// <summary>
    /// Computes a hardware-capability score (0-100) that reflects the ACTUAL
    /// power of this specific PC. Two machines with very different GPUs/CPUs will
    /// receive clearly different scores.
    ///
    /// Weighting:
    ///   GPU tier      — 45 pts  (dominant factor for gaming)
    ///   CPU tier      — 30 pts  (cores + clock speed)
    ///   RAM amount    — 15 pts
    ///   Power plan    —  7 pts  (optimization bonus)
    ///   Game Mode     —  3 pts  (optimization bonus)
    ///   ─────────────────────────
    ///   Total max     — 100 pts
    /// </summary>
    private static (uint score, int cpuScore, int gpuScore, int ramScore) ComputeScore(
        Hardware.Info.CPU? cpu,
        string? gpuName,
        double memoryGb,
        string powerPlan,
        bool gameMode)
    {
        // --- GPU tier (0-45) ---
        var gpuScore = GpuTier(gpuName);  // returns 0-45

        // --- CPU tier (0-30) ---
        var cpuScore = CpuTier(cpu);      // returns 0-30

        // --- RAM tier (0-15) ---
        int ramScore;
        if (memoryGb >= 64)       ramScore = 15;
        else if (memoryGb >= 32)  ramScore = 12;
        else if (memoryGb >= 16)  ramScore = 8;
        else if (memoryGb >= 8)   ramScore = 4;
        else                      ramScore = 1;

        // --- Optimization bonuses (0-10 combined) ---
        var plan = powerPlan.ToLowerInvariant();
        int planBonus;
        if (plan.Contains("ultimate") || plan.Contains("god"))    planBonus = 7;
        else if (plan.Contains("high performance"))               planBonus = 5;
        else if (plan.Contains("balanced"))                       planBonus = 2;
        else                                                      planBonus = 0;

        int gameModeBonus = gameMode ? 3 : 0;

        var total = gpuScore + cpuScore + ramScore + planBonus + gameModeBonus;
        return ((uint)Math.Clamp(total, 0, 100), cpuScore, gpuScore, ramScore);
    }

    /// <summary>
    /// Maps the detected GPU name to a performance tier (0-45 points).
    /// Tiers are based on well-known GPU generations; unknown/integrated GPUs score low.
    ///
    ///  45  — RTX 5090 / RTX 5080 / top-of-gen flagship
    ///  40  — RTX 5070 Ti / RTX 4090 / RTX 4080 / RX 7900 XTX
    ///  35  — RTX 5070 / RTX 4070 Ti / RTX 4070 / RX 7900 / RX 7800 / Arc A770
    ///  28  — RTX 4060 Ti / RTX 3080 Ti / RTX 3080 / RX 6800 / RX 6750
    ///  22  — RTX 3070 Ti / RTX 3070 / RTX 2080 Ti / RTX 2080 / RX 6700 / RX 6600 XT
    ///  16  — RTX 3060 Ti / RTX 3060 / RTX 2070 / RTX 2060 / RX 6600 / RX 5700 XT
    ///  10  — RTX 1660 Ti / GTX 1660 / RTX 2060 (12 GB) / RX 5600 XT / RX 5500 XT
    ///   6  — GTX 1650 Ti / GTX 1650 / GTX 1060 / RX 580 / RX 570 / Arc A380
    ///   2  — GTX 1050 Ti / GTX 1050 / RX 560 / GT 1030 / RX Vega iGPU / Intel UHD
    ///   0  — unknown / no discrete GPU
    /// </summary>
    internal static int GpuTier(string? gpuName)
    {
        if (string.IsNullOrWhiteSpace(gpuName))
        {
            return 0;
        }

        var n = gpuName.ToUpperInvariant();

        // ---- NVIDIA RTX 50xx series ----
        if (Contains(n, "RTX 5090") || Contains(n, "RTX5090")) return 45;
        if (Contains(n, "RTX 5080") || Contains(n, "RTX5080")) return 44;
        if (Contains(n, "RTX 5070 TI") || Contains(n, "RTX5070TI") || Contains(n, "RTX 5070TI")) return 40;
        if (Contains(n, "RTX 5070") || Contains(n, "RTX5070")) return 37;
        if (Contains(n, "RTX 5060 TI") || Contains(n, "RTX5060TI")) return 33;
        if (Contains(n, "RTX 5060") || Contains(n, "RTX5060")) return 28;

        // ---- NVIDIA RTX 40xx series ----
        if (Contains(n, "RTX 4090") || Contains(n, "RTX4090")) return 43;
        if (Contains(n, "RTX 4080") || Contains(n, "RTX4080")) return 40;
        if (Contains(n, "RTX 4070 TI") || Contains(n, "RTX4070TI") || Contains(n, "RTX 4070TI")) return 36;
        if (Contains(n, "RTX 4070") || Contains(n, "RTX4070")) return 34;
        if (Contains(n, "RTX 4060 TI") || Contains(n, "RTX4060TI") || Contains(n, "RTX 4060TI")) return 28;
        if (Contains(n, "RTX 4060") || Contains(n, "RTX4060")) return 24;
        if (Contains(n, "RTX 4050") || Contains(n, "RTX4050")) return 19;

        // ---- NVIDIA RTX 30xx series ----
        if (Contains(n, "RTX 3090") || Contains(n, "RTX3090")) return 38;
        if (Contains(n, "RTX 3080 TI") || Contains(n, "RTX3080TI") || Contains(n, "RTX 3080TI")) return 35;
        if (Contains(n, "RTX 3080") || Contains(n, "RTX3080")) return 33;
        if (Contains(n, "RTX 3070 TI") || Contains(n, "RTX3070TI") || Contains(n, "RTX 3070TI")) return 28;
        if (Contains(n, "RTX 3070") || Contains(n, "RTX3070")) return 26;
        if (Contains(n, "RTX 3060 TI") || Contains(n, "RTX3060TI") || Contains(n, "RTX 3060TI")) return 22;
        if (Contains(n, "RTX 3060") || Contains(n, "RTX3060")) return 19;
        if (Contains(n, "RTX 3050") || Contains(n, "RTX3050")) return 15;

        // ---- NVIDIA RTX 20xx series ----
        if (Contains(n, "RTX 2080 TI") || Contains(n, "RTX2080TI") || Contains(n, "RTX 2080TI")) return 28;
        if (Contains(n, "RTX 2080") || Contains(n, "RTX2080")) return 25;
        if (Contains(n, "RTX 2070 SUPER") || Contains(n, "RTX 2070S")) return 23;
        if (Contains(n, "RTX 2070") || Contains(n, "RTX2070")) return 21;
        if (Contains(n, "RTX 2060 SUPER") || Contains(n, "RTX 2060S")) return 19;
        if (Contains(n, "RTX 2060") || Contains(n, "RTX2060")) return 17;

        // ---- NVIDIA GTX 16xx / GTX 10xx ----
        if (Contains(n, "GTX 1660 TI") || Contains(n, "GTX1660TI") || Contains(n, "GTX 1660 SUPER")) return 13;
        if (Contains(n, "GTX 1660") || Contains(n, "GTX1660")) return 11;
        if (Contains(n, "GTX 1650 SUPER")) return 10;
        if (Contains(n, "GTX 1650") || Contains(n, "GTX1650")) return 7;
        if (Contains(n, "GTX 1080 TI") || Contains(n, "GTX1080TI")) return 18;
        if (Contains(n, "GTX 1080") || Contains(n, "GTX1080")) return 15;
        if (Contains(n, "GTX 1070 TI") || Contains(n, "GTX1070TI")) return 13;
        if (Contains(n, "GTX 1070") || Contains(n, "GTX1070")) return 12;
        if (Contains(n, "GTX 1060") || Contains(n, "GTX1060")) return 9;
        if (Contains(n, "GTX 1050 TI") || Contains(n, "GTX1050TI")) return 6;
        if (Contains(n, "GTX 1050") || Contains(n, "GTX1050")) return 5;
        if (Contains(n, "GT 1030") || Contains(n, "GT1030")) return 3;

        // ---- AMD RX 7000 series ----
        if (Contains(n, "RX 7900 XTX")) return 41;
        if (Contains(n, "RX 7900 XT") && !Contains(n, "XTX")) return 38;
        if (Contains(n, "RX 7900 GRE")) return 35;
        if (Contains(n, "RX 7800 XT")) return 32;
        if (Contains(n, "RX 7700 XT")) return 28;
        if (Contains(n, "RX 7600") || Contains(n, "RX7600")) return 22;

        // ---- AMD RX 6000 series ----
        if (Contains(n, "RX 6950 XT")) return 37;
        if (Contains(n, "RX 6900 XT")) return 36;
        if (Contains(n, "RX 6800 XT")) return 34;
        if (Contains(n, "RX 6800") && !Contains(n, "XT")) return 31;
        if (Contains(n, "RX 6750 XT")) return 28;
        if (Contains(n, "RX 6700 XT")) return 26;
        if (Contains(n, "RX 6700") && !Contains(n, "XT")) return 23;
        if (Contains(n, "RX 6650 XT")) return 21;
        if (Contains(n, "RX 6600 XT")) return 19;
        if (Contains(n, "RX 6600") && !Contains(n, "XT")) return 17;
        if (Contains(n, "RX 6500 XT")) return 10;

        // ---- AMD RX 5000 series ----
        if (Contains(n, "RX 5700 XT")) return 20;
        if (Contains(n, "RX 5700") && !Contains(n, "XT")) return 18;
        if (Contains(n, "RX 5600 XT")) return 15;
        if (Contains(n, "RX 5500 XT")) return 11;

        // ---- AMD older / Vega ----
        if (Contains(n, "RX 590") || Contains(n, "RX 580") || Contains(n, "RX 570")) return 9;
        if (Contains(n, "RX VEGA 64") || Contains(n, "VEGA 64")) return 18;
        if (Contains(n, "RX VEGA 56") || Contains(n, "VEGA 56")) return 15;

        // ---- Intel Arc discrete ----
        if (Contains(n, "ARC A770")) return 20;
        if (Contains(n, "ARC A750")) return 17;
        if (Contains(n, "ARC A580")) return 14;
        if (Contains(n, "ARC A380")) return 8;
        if (Contains(n, "ARC A310")) return 5;
        if (Contains(n, "ARC B580")) return 22;
        if (Contains(n, "ARC B570")) return 19;

        // ---- Integrated / iGPU — always low ----
        // AMD Radeon integrated (Ryzen iGPU)
        if (Contains(n, "RADEON 780M") || Contains(n, "RADEON 760M")) return 4;
        if (Contains(n, "RADEON 680M") || Contains(n, "RADEON 660M")) return 3;
        if (Contains(n, "RADEON VEGA") || Contains(n, "VEGA 8") || Contains(n, "VEGA 11")) return 2;
        if (Contains(n, "RADEON GRAPHICS"))  return 2; // generic Ryzen iGPU string

        // Intel integrated
        if (Contains(n, "INTEL UHD") || Contains(n, "INTEL HD")) return 2;
        if (Contains(n, "IRIS XE") || Contains(n, "IRIS PRO") || Contains(n, "IRIS PLUS")) return 3;

        // Any other discrete NVIDIA/AMD/Intel Arc we didn't specifically match
        if (Contains(n, "GEFORCE") || Contains(n, "NVIDIA")) return 5;
        if (Contains(n, "RADEON") || Contains(n, "AMD")) return 5;
        if (Contains(n, "ARC") && Contains(n, "INTEL")) return 5;

        return 0;
    }

    /// <summary>
    /// Maps CPU core count and clock speed to a tier (0-30 points).
    ///
    ///  CPU points (0-20) from physical cores:
    ///   16+ cores  — 20
    ///   12 cores   — 17
    ///    8 cores   — 14
    ///    6 cores   — 10
    ///    4 cores   —  6
    ///   &lt;4 cores   —  3
    ///
    ///  Clock speed bonus (0-10):
    ///   >= 4.5 GHz — 10
    ///   >= 4.0 GHz —  8
    ///   >= 3.5 GHz —  5
    ///   >= 3.0 GHz —  3
    ///   &lt;3.0 GHz   —  1
    /// </summary>
    private static int CpuTier(Hardware.Info.CPU? cpu)
    {
        if (cpu is null)
        {
            return 0;
        }

        var physicalCores = (int)(cpu.NumberOfCores > 0 ? cpu.NumberOfCores : 1);

        int corePoints;
        if (physicalCores >= 16)     corePoints = 20;
        else if (physicalCores >= 12) corePoints = 17;
        else if (physicalCores >= 8)  corePoints = 14;
        else if (physicalCores >= 6)  corePoints = 10;
        else if (physicalCores >= 4)  corePoints = 6;
        else                          corePoints = 3;

        // Use CurrentClockSpeed if available; fall back to MaxClockSpeed.
        var clockMhz = cpu.CurrentClockSpeed > 0 ? cpu.CurrentClockSpeed
                      : cpu.MaxClockSpeed > 0    ? cpu.MaxClockSpeed
                      : 0u;
        var clockGhz = clockMhz / 1000.0;

        int clockBonus;
        if (clockGhz >= 4.5)      clockBonus = 10;
        else if (clockGhz >= 4.0) clockBonus = 8;
        else if (clockGhz >= 3.5) clockBonus = 5;
        else if (clockGhz >= 3.0) clockBonus = 3;
        else if (clockGhz > 0)    clockBonus = 1;
        else                      clockBonus = 0;

        return Math.Min(corePoints + clockBonus, 30);
    }

    // ------------------------------------------------------------------
    // Misc private helpers
    // ------------------------------------------------------------------

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

    /// <summary>Helper to avoid repeated .Contains() calls with OrdinalIgnoreCase.</summary>
    private static bool Contains(string haystack, string needle) =>
        haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
