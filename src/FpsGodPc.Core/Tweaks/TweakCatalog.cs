using FpsGodPc.Core.Models;

namespace FpsGodPc.Core.Tweaks;

public static class TweakCatalog
{
    public static IReadOnlyList<TweakDefinition> All { get; } =
    [
        Def("win-game-mode",           "Enable Game Mode",                   "Prioritizes game processes and reduces background interruptions via HKCU\\GameBar\\AutoGameModeEnabled.", "windows", RiskTier.Safe, false),
        Def("win-power-high",          "High Performance Power Plan",        "Switches to the built-in High Performance plan (8c5e7fda-…) so CPU and GPU stay at full frequency.", "windows", RiskTier.Safe, true),
        Def("win-visual-fx",           "Disable Visual Effects",             "Sets VisualFXSetting=2 to turn off animations and transparency, freeing CPU/GPU cycles.", "windows", RiskTier.Safe, false),
        Def("win-game-dvr",            "Disable Xbox Game Bar DVR",          "Disables GameDVR_Enabled and AppCaptureEnabled to stop background recording overhead.", "windows", RiskTier.Moderate, true),
        Def("win-telemetry",           "Disable Telemetry Services",         "Stops and disables DiagTrack and dmwappushservice. Requires Administrator.", "windows", RiskTier.Moderate, true),
        Def("win-fullscreen-opt",      "Disable Fullscreen Optimizations",   "Sets GameConfigStore flags to opt out of DXGI fullscreen optimizations globally.", "windows", RiskTier.Moderate, true),
        Def("win-bg-apps",             "Limit Background Apps",              "Sets Disabled=1 and DisabledByUser=1 on every HKCU BackgroundAccessApplications entry. Revert restores both values to 0.", "windows", RiskTier.Safe, false),
        Def("win-disable-power-saving","Disable All Power Saving",           "Applies six powercfg AC/DC value overrides to eliminate CPU/GPU power throttling on the active plan.", "windows", RiskTier.Moderate, true),
        Def("win-priority-26",         "Win32 Priority Separation (0x26)",   "Writes Win32PrioritySeparation=0x26 to HKLM PriorityControl for a gaming CPU scheduler profile. Real prior value is captured and restored on revert.", "windows", RiskTier.Moderate, true),
        Def("win-mmcss-latency",       "MMCSS Gaming Profile",               "Sets SystemResponsiveness=0 and configures the MMCSS Games task (GPU Priority 8, Priority 6, Scheduling Category High). Real prior value is captured and restored on revert.", "windows", RiskTier.Moderate, true),
        Def("win-system-ini-fps",      "system.ini Latency Profile (Guidance Only)", "Legacy [386Enh] time-slice entries are ignored by modern 64-bit Windows, and editing %WINDIR%\\system.ini can trip game anti-cheat (it crashed Apex/EA AntiCheat). Not applied automatically — guidance only.", "windows", RiskTier.Advanced, false, advisorOnly: true),
        Def("gpu-shader-cache",        "Clear DirectX Shader Cache",         "Removes stale files from D3DSCache, NVIDIA DXCache, and AMD DxCache folders. One-shot — no revert.", "gpu", RiskTier.Safe, false, reversible: false),
        Def("gpu-max-perf",            "Prefer Maximum Performance (Guidance Only)", "Set Power management mode to 'Prefer maximum performance' in NVIDIA Control Panel → Manage 3D Settings (or the AMD/Intel control panel). Not automated — writing the driver registry directly can destabilize the driver and trip game anti-cheat, so this is guidance only.", "gpu", RiskTier.Advanced, false, advisorOnly: true),
        Def("gpu-low-latency",         "Low Latency Mode (Guidance Only)",   "Enable 'Low Latency Mode = Ultra' in NVIDIA Control Panel (or Radeon Anti-Lag / Intel equivalent). Not automated — the old registry approach used an undocumented value that could crash anti-cheat games, so this is guidance only.", "gpu", RiskTier.Advanced, false, advisorOnly: true),
        Advisor("gpu-hags-advisor",    "HAGS Status Check",                  "Scans Hardware Accelerated GPU Scheduling — guidance only, no automated changes.", "gpu"),
        Def("gpu-power-limit",         "GPU Power Limit (Guidance Only)",    "Adjusting GPU power limits requires vendor SDK tooling (MSI Afterburner, GPU-Z, etc.). This entry is guidance only — no automated changes are made.", "gpu", RiskTier.Advanced, true, advisorOnly: true),
        Def("gpu-clock-offset",        "GPU Clock Offset (Guidance Only)",   "Core/memory offset tuning requires vendor tools and careful stability testing. This entry is guidance only — no automated changes are made.", "gpu", RiskTier.Advanced, true, advisorOnly: true),
        Def("cpu-game-priority",       "Game Process High Priority",         "Uses System.Diagnostics.Process.PriorityClass to raise detected game processes to High. Applied immediately to any currently-running game processes; GameWatcher applies it on each game launch while this tweak is active.", "cpu", RiskTier.Safe, true),
        Def("cpu-core-parking",        "Disable CPU Core Parking",           "Runs powercfg to set core parking minimum to 0% on the active plan, keeping all cores available.", "cpu", RiskTier.Moderate, true),
        Def("cpu-timer-res",           "Timer Resolution (0.5 ms)",          "Calls NtSetTimerResolution via P/Invoke to request ~0.5 ms scheduling interval for this process. Resolution is held for the app session and released on revert. GameWatcher also holds it while a watched game runs.", "cpu", RiskTier.Moderate, true),
        Advisor("cpu-undervolt",       "CPU Undervolt (Guidance Only)",      "Undervolting requires Intel XTU, AMD Ryzen Master, or BIOS settings. This entry is guidance only — no automated changes are made.", "cpu"),
        Def("cpu-power-limit",         "CPU Power Limit (Guidance Only)",    "PL1/PL2 tuning requires Intel XTU or AMD Ryzen Master. This entry is guidance only — no automated changes are made.", "cpu", RiskTier.Advanced, true, advisorOnly: true),
        Def("net-dns-flush",           "Flush DNS Cache",                    "Runs ipconfig /flushdns to clear the resolver cache. One-shot — no revert needed.", "network", RiskTier.Safe, true, reversible: false),
        Def("net-adapter-power",       "Disable Adapter Power Saving",       "Runs Disable-NetAdapterPowerManagement on all physical adapters to prevent NIC sleep.", "network", RiskTier.Safe, true),
        Def("net-nagle",               "Disable Nagle's Algorithm",          "Sets TcpAckFrequency=1 and TCPNoDelay=1 on all Tcpip interface subkeys to reduce TCP buffering latency.", "network", RiskTier.Moderate, true),
        Def("net-throttling",          "Network Throttling Index",           "Sets NetworkThrottlingIndex=0xFFFFFFFF (disabled) in the Multimedia SystemProfile. Real prior value is captured and restored on revert.", "network", RiskTier.Moderate, true),
        Def("adv-fan-curve",           "Fan Curve Tuning (Guidance Only)",   "Manual fan curve tuning requires vendor tools (MSI Center, ASUS Armoury Crate, etc.). This entry is guidance only — no automated changes are made.", "advanced", RiskTier.Advanced, true, advisorOnly: true),
        Def("adv-ram-standby",         "Standby Memory Cleaner",             "Calls NtSetSystemInformation to flush the standby list, freeing RAM immediately. One-shot — no revert.", "advanced", RiskTier.Moderate, true),
        Advisor("adv-vbs-warn",        "VBS / Core Isolation Check",         "Detects whether Virtualization Based Security is active — guidance only, no automated changes.", "advanced"),
        Def("adv-vbs-disable",         "Disable Memory Integrity (Guidance Only)", "Disabling HVCI/Memory Integrity requires a registry change and reboot; also reduces security. This entry is guidance only — no automated changes are made.", "advanced", RiskTier.Advanced, true, advisorOnly: true),
        Advisor("adv-bios-xmp",        "XMP / EXPO Advisor",                 "Checks if RAM runs below its rated speed. XMP/EXPO must be enabled in BIOS — guidance only, no automated changes.", "advanced"),
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
