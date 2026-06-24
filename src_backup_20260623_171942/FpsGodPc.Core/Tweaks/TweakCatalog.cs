using FpsGodPc.Core.Models;

namespace FpsGodPc.Core.Tweaks;

public static class TweakCatalog
{
    public static IReadOnlyList<TweakDefinition> All { get; } =
    [
        Def("win-game-mode", "Enable Game Mode", "Prioritizes game processes and reduces background interruptions.", "windows", RiskTier.Safe, false),
        Def("win-power-high", "High Performance Power Plan", "Switches to High Performance so CPU and GPU stay responsive.", "windows", RiskTier.Safe, true),
        Def("win-visual-fx", "Disable Visual Effects", "Turns off animations and transparency to free CPU/GPU cycles.", "windows", RiskTier.Safe, false),
        Def("win-game-dvr", "Disable Xbox Game Bar DVR", "Stops background recording overhead that can hurt FPS.", "windows", RiskTier.Moderate, true),
        Def("win-telemetry", "Disable Telemetry Services", "Reduces background data collection services.", "windows", RiskTier.Moderate, true),
        Def("win-fullscreen-opt", "Disable Fullscreen Optimizations", "Can lower input lag in some titles.", "windows", RiskTier.Moderate, true),
        Def("win-bg-apps", "Limit Background Apps", "Prevents non-essential UWP apps from running in the background.", "windows", RiskTier.Safe, false),
        Def("win-disable-power-saving", "Disable All Power Saving", "Max performance on active plan.", "windows", RiskTier.Moderate, true),
        Def("win-priority-26", "Win32 Priority Separation (0x26)", "Gaming CPU scheduler profile (0x26).", "windows", RiskTier.Moderate, true),
        Def("win-mmcss-latency", "MMCSS Gaming Profile", "System Responsiveness 0, Games task high priority.", "windows", RiskTier.Moderate, true),
        Def("win-system-ini-fps", "system.ini Latency Profile", "Applies [386Enh] time-slice latency tweaks.", "windows", RiskTier.Moderate, true),
        Def("gpu-shader-cache", "Clear DirectX Shader Cache", "Removes stale shader cache that can cause stutter.", "gpu", RiskTier.Safe, false, reversible: false),
        Def("gpu-max-perf", "Prefer Maximum Performance", "Sets NVIDIA/AMD power management to maximum performance.", "gpu", RiskTier.Moderate, true),
        Def("gpu-low-latency", "Low Latency Mode", "Enables driver low-latency path where supported.", "gpu", RiskTier.Moderate, true),
        Advisor("gpu-hags-advisor", "HAGS Status Check", "Scans Hardware Accelerated GPU Scheduling.", "gpu"),
        Def("gpu-power-limit", "GPU Power Limit", "Adjusts power limit via vendor SDK.", "gpu", RiskTier.Advanced, true, advisorOnly: true),
        Def("gpu-clock-offset", "GPU Clock Offset", "Core/memory offset tuning.", "gpu", RiskTier.Advanced, true, advisorOnly: true),
        Def("cpu-game-priority", "Game Process High Priority", "Raises active game process priority while session runs.", "cpu", RiskTier.Safe, true),
        Def("cpu-core-parking", "Disable CPU Core Parking", "Keeps all cores awake for lower latency.", "cpu", RiskTier.Moderate, true),
        Def("cpu-timer-res", "Timer Resolution (Gaming)", "Requests 0.5ms timer while gaming.", "cpu", RiskTier.Moderate, true),
        Advisor("cpu-undervolt", "CPU Undervolt Guide", "Step-by-step advisor only.", "cpu"),
        Def("cpu-power-limit", "CPU Power Limit (PL1/PL2)", "Wraps vendor tooling for power limits.", "cpu", RiskTier.Advanced, true, advisorOnly: true),
        Def("net-dns-flush", "Flush DNS Cache", "Clears resolver cache.", "network", RiskTier.Safe, true, reversible: false),
        Def("net-adapter-power", "Disable Adapter Power Saving", "Prevents NIC from sleeping.", "network", RiskTier.Safe, true),
        Def("net-nagle", "Disable Nagle's Algorithm", "May reduce latency in some online games.", "network", RiskTier.Moderate, true),
        Def("net-throttling", "Network Throttling Index", "Tunes Windows multimedia network throttling.", "network", RiskTier.Moderate, true),
        Def("adv-fan-curve", "Fan Curve Tuning", "Manual fan curve via vendor tools.", "advanced", RiskTier.Advanced, true, advisorOnly: true),
        Def("adv-ram-standby", "Standby Memory Cleaner", "Flushes standby list on demand.", "advanced", RiskTier.Moderate, true),
        Advisor("adv-vbs-warn", "VBS / Core Isolation Check", "Detects virtualization security features.", "advanced"),
        Def("adv-vbs-disable", "Disable Memory Integrity", "Can improve FPS on some CPUs.", "advanced", RiskTier.Advanced, true, advisorOnly: true),
        Advisor("adv-bios-xmp", "XMP / EXPO Advisor", "Checks if RAM runs below rated speed.", "advanced"),
    ];

    public static IReadOnlyList<string> AllIds { get; } = All.Select(t => t.Id).ToList();
    public static uint ApplicableCount => (uint)All.Count(t => !t.AdvisorOnly);
    public static TweakDefinition? Get(string id) => All.FirstOrDefault(t => t.Id == id);

    public static bool IsAdvisorOnly(string id) =>
        Get(id)?.AdvisorOnly == true ||
        id is "gpu-power-limit" or "gpu-clock-offset" or "cpu-power-limit" or "adv-fan-curve" or "adv-vbs-disable";

    private static TweakDefinition Def(string id, string name, string desc, string category, RiskTier tier, bool admin, bool advisorOnly = false, bool reversible = true) => new()
    {
        Id = id, Name = name, Description = desc, Category = category, Tier = tier,
        RequiresAdmin = admin, AdvisorOnly = advisorOnly, Reversible = reversible,
    };

    private static TweakDefinition Advisor(string id, string name, string desc, string category) => new()
    {
        Id = id, Name = name, Description = desc, Category = category, Tier = RiskTier.Safe, AdvisorOnly = true,
    };
}
