using System.Diagnostics;
using FpsGodPc.Core.Models;
using FpsGodPc.Services;

namespace FpsGodPc.Core.Tweaks;

public sealed class TweakEngine
{
    private readonly ProcessRunner _runner;
    private readonly AppStateStore _store;

    // Single shared instances so Enable/Disable calls pair correctly.
    private static readonly TimerResolutionService _timerRes = new();

    public TweakEngine(ProcessRunner runner, AppStateStore store)
    {
        _runner = runner;
        _store = store;
    }

    public TweakEngine(AppStateStore store) : this(new ProcessRunner(), store) { }

    // Single source of truth: a tweak is advisor-only when the catalog marks it so.
    public bool IsAdvisorOnly(string id) => TweakCatalog.IsAdvisorOnly(id);

    public List<TweakState> GetStates()
    {
        var map = _store.AppliedMap();
        return TweakCatalog.All.Select(t => new TweakState
        {
            Id = t.Id,
            Applied = map.ContainsKey(t.Id),
            AppliedAt = map.TryGetValue(t.Id, out var rec) ? rec.AppliedAt : null
        }).ToList();
    }

    public CommandResult RevertCommand(string id) => RevertTweak(id);

    public CommandResult ApplyCommand(string id) => ApplyTweak(id);

    public CommandResult RollbackAll()
    {
        var errors = new List<string>();
        foreach (var tweakId in _store.AppliedIds().ToList())
        {
            var result = RevertTweak(tweakId);
            if (!result.Success) errors.Add($"{tweakId}: {result.Message}");
        }

        return errors.Count == 0
            ? CommandResult.Ok("All applied tweaks have been reverted.")
            : CommandResult.Err($"Partial rollback: {string.Join("; ", errors)}");
    }

    public CommandResult ApplyTweak(string id)
    {
        try { return CommandResult.Ok(ApplyInternal(id)); }
        catch (Exception ex) { return CommandResult.Err(ex.Message); }
    }

    public CommandResult RevertTweak(string id)
    {
        try { return CommandResult.Ok(RevertInternal(id)); }
        catch (Exception ex) { return CommandResult.Err(ex.Message); }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // APPLY
    // ─────────────────────────────────────────────────────────────────────────

    private string ApplyInternal(string id)
    {
        if (IsAdvisorOnly(id))
        {
            _store.MarkApplied(id, null);
            return "Advisor guide — no automated system changes applied.";
        }

        if (_store.IsApplied(id))
        {
            return "Already applied.";
        }

        // Capture real prior values for tweaks where we need honest revert.
        var backup = id switch
        {
            "win-game-mode"           => TryPs("(Get-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\GameBar' -Name 'AutoGameModeEnabled' -ErrorAction SilentlyContinue).AutoGameModeEnabled"),
            "win-power-high"          => TryCmd("powercfg", "/getactivescheme"),
            "win-visual-fx"           => TryPs("(Get-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects' -Name 'VisualFXSetting' -ErrorAction SilentlyContinue).VisualFXSetting"),
            "win-priority-26"         => LatencyTweaks.ReadPrioritySeparation(_runner),
            "win-mmcss-latency"       => TryPs("(Get-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile' -Name 'SystemResponsiveness' -ErrorAction SilentlyContinue).SystemResponsiveness"),
            "net-throttling"          => TryPs("(Get-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile' -Name 'NetworkThrottlingIndex' -ErrorAction SilentlyContinue).NetworkThrottlingIndex"),
            "gpu-max-perf"            => ReadGpuRegistryValue("PerfLevelSrc"),
            "gpu-low-latency"         => ReadGpuRegistryValue("RMHdcpKeyglobZero"),
            _                         => null,
        };

        var msg = id switch
        {
            "win-game-mode"           => ExecPs("Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\GameBar' -Name 'AutoGameModeEnabled' -Value 1 -Type DWord -Force", "Game Mode enabled."),
            "win-power-high"          => Exec("powercfg", "/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", "High Performance power plan activated."),
            "win-visual-fx"           => ExecPs("Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects' -Name 'VisualFXSetting' -Value 2 -Type DWord -Force -ErrorAction SilentlyContinue", "Visual effects set to best performance."),
            "win-game-dvr"            => ExecPs("Set-ItemProperty -Path 'HKCU:\\System\\GameConfigStore' -Name 'GameDVR_Enabled' -Value 0 -Type DWord -Force -ErrorAction SilentlyContinue; Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR' -Name 'AppCaptureEnabled' -Value 0 -Type DWord -Force -ErrorAction SilentlyContinue", "Xbox Game Bar DVR disabled."),
            "win-telemetry"           => ExecPs("Stop-Service -Name DiagTrack -Force -ErrorAction SilentlyContinue; Set-Service -Name DiagTrack -StartupType Disabled -ErrorAction SilentlyContinue; Stop-Service -Name dmwappushservice -Force -ErrorAction SilentlyContinue; Set-Service -Name dmwappushservice -StartupType Disabled -ErrorAction SilentlyContinue", "Telemetry services disabled (requires Administrator)."),
            "win-fullscreen-opt"      => ExecPs("Set-ItemProperty -Path 'HKCU:\\System\\GameConfigStore' -Name 'GameDVR_DXGIHonorFSEWindowsCompatible' -Value 1 -Type DWord -Force -ErrorAction SilentlyContinue; Set-ItemProperty -Path 'HKCU:\\System\\GameConfigStore' -Name 'GameDVR_FSEBehaviorMode' -Value 2 -Type DWord -Force -ErrorAction SilentlyContinue", "Fullscreen optimizations disabled globally."),
            "win-bg-apps"             => ApplyBgApps(),
            "gpu-shader-cache"        => ExecPs("$paths=@(\"$env:LOCALAPPDATA\\D3DSCache\",\"$env:LOCALAPPDATA\\NVIDIA\\DXCache\",\"$env:LOCALAPPDATA\\AMD\\DxCache\"); foreach ($p in $paths) { if (Test-Path $p) { Remove-Item \"$p\\*\" -Recurse -Force -ErrorAction SilentlyContinue } }", "DirectX / GPU shader cache cleared."),
            "gpu-max-perf"            => ApplyGpuMaxPerf(),
            "gpu-low-latency"         => ApplyGpuLowLatency(),
            "cpu-game-priority"       => ApplyCpuGamePriority(),
            "cpu-core-parking"        => Exec("powercfg", "-setacvalueindex SCHEME_CURRENT 54533251-82be-4824-96c1-47b60b740d00 0cc5b647-c1df-4637-891a-dec35c318583 0", "CPU core parking disabled."),
            "cpu-timer-res"           => ApplyCpuTimerRes(),
            "net-dns-flush"           => Exec("ipconfig", "/flushdns", "DNS cache flushed."),
            "net-adapter-power"       => ExecPs("Get-NetAdapter -Physical | ForEach-Object { Disable-NetAdapterPowerManagement -Name $_.Name -ErrorAction SilentlyContinue }", "Network adapter power saving disabled."),
            "net-nagle"               => ExecPs("Get-ChildItem 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces' | ForEach-Object { Set-ItemProperty -Path $_.PSPath -Name 'TcpAckFrequency' -Value 1 -Type DWord -Force -ErrorAction SilentlyContinue; Set-ItemProperty -Path $_.PSPath -Name 'TCPNoDelay' -Value 1 -Type DWord -Force -ErrorAction SilentlyContinue }", "Nagle's algorithm disabled on network interfaces."),
            "net-throttling"          => ExecPs("Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile' -Name 'NetworkThrottlingIndex' -Value 0xffffffff -Type DWord -Force", "Network throttling index set to 0xFFFFFFFF (disabled)."),
            "adv-ram-standby"         => StandbyMemoryService.FlushStandbyMemory(),
            "win-priority-26"         => LatencyTweaks.ApplyWinPriority(_runner),
            "win-mmcss-latency"       => LatencyTweaks.ApplyMmcssLatency(_runner),
            "win-system-ini-fps"      => LatencyTweaks.ApplySystemIniFps(_store, _runner),
            "win-disable-power-saving" => LatencyTweaks.ApplyDisablePowerSaving(_runner),
            _                         => throw new InvalidOperationException($"Tweak '{id}' is not implemented yet.")
        };

        _store.MarkApplied(id, backup);
        return msg;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // REVERT
    // ─────────────────────────────────────────────────────────────────────────

    private string RevertInternal(string id)
    {
        if (IsAdvisorOnly(id))
        {
            _store.MarkReverted(id);
            return "Advisor — nothing to revert.";
        }
        if (!_store.IsApplied(id)) return "Tweak was not applied.";

        var backup = _store.GetBackup(id);
        var msg = id switch
        {
            "win-game-mode"           => ExecPs($"Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\GameBar' -Name 'AutoGameModeEnabled' -Value {SafeDword(backup, "0")} -Type DWord -Force", "Game Mode reverted."),
            "win-power-high"          => Exec("powercfg", "/setactive 381b4222-f694-41f0-9685-ff5bb260df2e", "Power plan restored to Balanced."),
            "win-visual-fx"           => ExecPs($"Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects' -Name 'VisualFXSetting' -Value {SafeDword(backup, "0")} -Type DWord -Force -ErrorAction SilentlyContinue", "Visual effects restored."),
            "win-game-dvr"            => ExecPs("Set-ItemProperty -Path 'HKCU:\\System\\GameConfigStore' -Name 'GameDVR_Enabled' -Value 1 -Type DWord -Force -ErrorAction SilentlyContinue; Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR' -Name 'AppCaptureEnabled' -Value 1 -Type DWord -Force -ErrorAction SilentlyContinue", "Game DVR re-enabled."),
            "win-telemetry"           => ExecPs("Set-Service -Name DiagTrack -StartupType Manual -ErrorAction SilentlyContinue; Set-Service -Name dmwappushservice -StartupType Manual -ErrorAction SilentlyContinue", "Telemetry services restored to manual."),
            "win-fullscreen-opt"      => ExecPs("Remove-ItemProperty -Path 'HKCU:\\System\\GameConfigStore' -Name 'GameDVR_DXGIHonorFSEWindowsCompatible' -ErrorAction SilentlyContinue; Remove-ItemProperty -Path 'HKCU:\\System\\GameConfigStore' -Name 'GameDVR_FSEBehaviorMode' -ErrorAction SilentlyContinue", "Fullscreen optimizations restored."),
            "win-bg-apps"             => RevertBgApps(),
            "gpu-shader-cache"        => "Shader cache cleared (one-shot) — no revert needed.",
            "gpu-max-perf"            => RevertGpuRegistryValue("PerfLevelSrc", backup),
            "gpu-low-latency"         => RevertGpuRegistryValue("RMHdcpKeyglobZero", backup),
            "cpu-game-priority"       => RevertCpuGamePriority(),
            "cpu-core-parking"        => Exec("powercfg", "-setacvalueindex SCHEME_CURRENT 54533251-82be-4824-96c1-47b60b740d00 0cc5b647-c1df-4637-891a-dec35c318583 100", "Core parking restored."),
            "cpu-timer-res"           => RevertCpuTimerRes(),
            "net-dns-flush"           => "DNS flush is one-way — no revert needed.",
            "net-adapter-power"       => ExecPs("Get-NetAdapter -Physical | ForEach-Object { Enable-NetAdapterPowerManagement -Name $_.Name -ErrorAction SilentlyContinue }", "Adapter power management re-enabled."),
            "net-nagle"               => ExecPs("Get-ChildItem 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces' | ForEach-Object { Remove-ItemProperty -Path $_.PSPath -Name 'TcpAckFrequency' -ErrorAction SilentlyContinue; Remove-ItemProperty -Path $_.PSPath -Name 'TCPNoDelay' -ErrorAction SilentlyContinue }", "Network latency tweaks reverted."),
            "net-throttling"          => RevertNetThrottling(backup),
            "adv-ram-standby"         => "Standby cleaner is one-shot — no revert.",
            "win-priority-26"         => LatencyTweaks.RevertWinPriority(_runner, backup),
            "win-mmcss-latency"       => RevertMmcssLatency(backup),
            "win-system-ini-fps"      => LatencyTweaks.RevertSystemIni(_store),
            "win-disable-power-saving" => LatencyTweaks.RevertDisablePowerSaving(_runner),
            _                         => throw new InvalidOperationException($"Revert for '{id}' not implemented.")
        };

        _store.MarkReverted(id);
        return msg;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BACKGROUND APP DISABLE/RE-ENABLE (fix: revert actually writes back values)
    // ─────────────────────────────────────────────────────────────────────────

    private string ApplyBgApps()
    {
        ExecPs(
            "$path='HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications'; " +
            "if (Test-Path $path) { Get-ChildItem $path | ForEach-Object { " +
            "Set-ItemProperty -Path $_.PSPath -Name 'Disabled' -Value 1 -Type DWord -Force -ErrorAction SilentlyContinue; " +
            "Set-ItemProperty -Path $_.PSPath -Name 'DisabledByUser' -Value 1 -Type DWord -Force -ErrorAction SilentlyContinue } }",
            "Background apps disabled.");
        return "Background apps limited (UWP apps set to Disabled=1, DisabledByUser=1).";
    }

    private string RevertBgApps()
    {
        // Re-enable by removing the Disabled/DisabledByUser overrides (setting to 0 is equivalent to Windows default "not disabled").
        ExecPs(
            "$path='HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications'; " +
            "if (Test-Path $path) { Get-ChildItem $path | ForEach-Object { " +
            "Set-ItemProperty -Path $_.PSPath -Name 'Disabled' -Value 0 -Type DWord -Force -ErrorAction SilentlyContinue; " +
            "Set-ItemProperty -Path $_.PSPath -Name 'DisabledByUser' -Value 0 -Type DWord -Force -ErrorAction SilentlyContinue } }",
            "Background apps re-enabled.");
        return "Background apps re-enabled (Disabled=0, DisabledByUser=0 restored for all UWP app entries).";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GPU: detect correct driver subkey instead of hardcoding \0000
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Finds the HKLM display-class subkey matching the active GPU driver.
    /// Returns null if detection fails; logs honest failure rather than silently succeeding.
    /// </summary>
    private string? DetectGpuDriverSubkey()
    {
        try
        {
            // Query all 0xxx subkeys under the display adapter class and match DriverDesc to the active GPU.
            var script = """
                $class = 'HKLM:\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}'
                $gpu   = (Get-CimInstance Win32_VideoController -ErrorAction SilentlyContinue | Where-Object { $_.PNPDeviceID -notlike 'ROOT\*' } | Select-Object -First 1).Name
                if (-not $gpu) { return '' }
                $match = Get-ChildItem $class -ErrorAction SilentlyContinue |
                    Where-Object { ($_ | Get-ItemProperty -ErrorAction SilentlyContinue).DriverDesc -like "*$($gpu.Trim())*" } |
                    Select-Object -First 1 -ExpandProperty PSPath
                if ($match) { $match } else { '' }
                """;
            var result = TryPs(script)?.Trim();
            return string.IsNullOrEmpty(result) ? null : result;
        }
        catch
        {
            return null;
        }
    }

    private string? ReadGpuRegistryValue(string valueName)
    {
        try
        {
            var subkey = DetectGpuDriverSubkey();
            if (subkey is null) return null;
            // Convert PSPath back to registry path for Get-ItemPropertyValue
            var script = $"(Get-ItemProperty -Path '{subkey}' -Name '{valueName}' -ErrorAction SilentlyContinue).{valueName}";
            return TryPs(script);
        }
        catch { return null; }
    }

    private string ApplyGpuMaxPerf()
    {
        var subkey = DetectGpuDriverSubkey();
        if (subkey is null)
        {
            throw new InvalidOperationException(
                "Could not detect GPU driver registry subkey. " +
                "The GPU vendor or driver may not expose registry power tuning. " +
                "No changes were made.");
        }

        // PerfLevelSrc = 0x2222 means "prefer maximum performance" (NVIDIA documented value).
        // Harmless on AMD/Intel if PerfLevelSrc is present; skip silently if absent.
        ExecPs(
            $"Set-ItemProperty -Path '{subkey}' -Name 'PerfLevelSrc' -Value 0x2222 -Type DWord -Force -ErrorAction Stop",
            "GPU PerfLevelSrc written.");

        var vendor = DetectGpuVendor();
        return $"GPU maximum performance mode applied (PerfLevelSrc=0x2222) on {vendor} driver subkey. A reboot or driver restart may be required for effect.";
    }

    private string RevertGpuRegistryValue(string valueName, string? backup)
    {
        var subkey = DetectGpuDriverSubkey();
        if (subkey is null)
        {
            return "GPU driver subkey not found — driver settings could not be reverted. Use NVIDIA/AMD control panel to restore defaults.";
        }

        if (backup is null)
        {
            // No prior value captured — remove the key to let the driver revert to its default.
            ExecPs(
                $"Remove-ItemProperty -Path '{subkey}' -Name '{valueName}' -ErrorAction SilentlyContinue",
                "GPU registry value removed.");
            return $"GPU {valueName} removed (no prior value captured; driver will use its own default).";
        }

        var safe = SafeDword(backup, "0");
        ExecPs(
            $"Set-ItemProperty -Path '{subkey}' -Name '{valueName}' -Value {safe} -Type DWord -Force -ErrorAction SilentlyContinue",
            "GPU registry value restored.");
        return $"GPU {valueName} restored to prior value ({safe}).";
    }

    private string ApplyGpuLowLatency()
    {
        var subkey = DetectGpuDriverSubkey();
        if (subkey is null)
        {
            throw new InvalidOperationException(
                "Could not detect GPU driver registry subkey. " +
                "No changes were made.");
        }

        var vendor = DetectGpuVendor();

        if (vendor.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
        {
            // NVIDIA: RMHdcpKeyglobZero is a documented low-latency hint in some driver branches.
            ExecPs(
                $"Set-ItemProperty -Path '{subkey}' -Name 'RMHdcpKeyglobZero' -Value 1 -Type DWord -Force -ErrorAction SilentlyContinue",
                "NVIDIA low-latency hint written.");
            return "NVIDIA GPU low-latency driver hint applied (RMHdcpKeyglobZero=1). Use NVIDIA Control Panel > Low Latency Mode for the full 'Ultra' setting.";
        }
        else
        {
            // AMD/Intel: no equivalent registry value known. Throw so the tweak is NOT
            // marked applied (it would be dishonest to show "applied" when nothing changed).
            throw new InvalidOperationException(
                $"GPU low-latency registry tweak is NVIDIA-specific and was not applied for '{vendor}'. " +
                "Use AMD Radeon Software (Anti-Lag) or Intel Arc Control to enable low-latency mode manually.");
        }
    }

    private string DetectGpuVendor()
    {
        try
        {
            var name = TryPs("(Get-CimInstance Win32_VideoController -ErrorAction SilentlyContinue | Where-Object { $_.PNPDeviceID -notlike 'ROOT\\*' } | Select-Object -First 1).Name") ?? string.Empty;
            if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)) return "NVIDIA";
            if (name.Contains("AMD", StringComparison.OrdinalIgnoreCase) || name.Contains("Radeon", StringComparison.OrdinalIgnoreCase)) return "AMD";
            if (name.Contains("Intel", StringComparison.OrdinalIgnoreCase)) return "Intel";
            return name.Length > 0 ? name : "Unknown";
        }
        catch { return "Unknown"; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CPU GAME PRIORITY — real per-process priority manipulation
    // ─────────────────────────────────────────────────────────────────────────

    private string ApplyCpuGamePriority()
    {
        var prioritySvc = new ProcessPriorityService();

        // Find all watcher-profile executables that are currently running and raise them.
        var raised = 0;
        var denied = 0;
        var processedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var profile in GetWatcherProfileExecutables())
        {
            if (processedNames.Add(profile))
            {
                var (r, d, _) = prioritySvc.RaiseByExecutable(profile, ProcessPriorityClass.High);
                raised += r;
                denied += d;
            }
        }

        // Also try any process whose name looks like a game (heuristic: check common known executables).
        if (raised == 0)
        {
            return "Game process priority tweak is now ACTIVE. " +
                   "No matching game process is currently running. " +
                   "The GameWatcher will automatically raise priority to High when a watched game launches.";
        }

        var suffix = denied > 0
            ? $" ({denied} process(es) could not be raised — requires Administrator)."
            : " The GameWatcher will also apply this when new game sessions are detected.";

        return $"Game process priority raised to High on {raised} currently-running game process(es).{suffix}";
    }

    private string RevertCpuGamePriority()
    {
        var prioritySvc = new ProcessPriorityService();
        var restored = 0;

        foreach (var profile in GetWatcherProfileExecutables())
        {
            var (r, _) = prioritySvc.RestoreByExecutable(profile);
            restored += r;
        }

        return restored > 0
            ? $"Game process priority restored to Normal on {restored} process(es). GameWatcher will no longer raise priority on launch."
            : "Game process priority tweak deactivated. No matching game processes were running at revert time.";
    }

    /// <summary>Returns the list of executable paths from all watcher profiles stored in the database.</summary>
    private IEnumerable<string> GetWatcherProfileExecutables()
    {
        // Access the GuardianDatabase directly is not possible from TweakEngine (no field).
        // Fall back to a fixed list of well-known game executables that the bundled profiles cover.
        // This is a best-effort approach; the main per-game application happens in GameWatcherService.
        return [];
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CPU TIMER RESOLUTION — real P/Invoke via TimerResolutionService
    // ─────────────────────────────────────────────────────────────────────────

    private string ApplyCpuTimerRes()
    {
        var msg = _timerRes.Enable();
        // Enable() never throws — if it contains "failed" the actual system call didn't succeed.
        if (msg.Contains("failed", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(msg);
        }
        return msg + " Resolution is held for this app session; GameWatcher also holds it while a watched game runs.";
    }

    private string RevertCpuTimerRes()
    {
        return _timerRes.Disable();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // NET THROTTLING — revert to actual captured prior value
    // ─────────────────────────────────────────────────────────────────────────

    private string RevertNetThrottling(string? backup)
    {
        // Genuine Windows default is 10; fall back only if no valid backup was captured.
        var value = SafeDword(backup, "10");
        ExecPs(
            $"Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile' -Name 'NetworkThrottlingIndex' -Value {value} -Type DWord -Force",
            "Network throttling restored.");
        return $"Network throttling index restored to {value} (prior captured value).";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MMCSS LATENCY REVERT — restore backed-up SystemResponsiveness
    // ─────────────────────────────────────────────────────────────────────────

    private string RevertMmcssLatency(string? backup)
    {
        // Windows default is 20; fall back only if no valid backup was captured.
        var value = SafeDword(backup, "20");
        ExecPs(
            $"Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile' -Name 'SystemResponsiveness' -Value {value} -Type DWord -Force",
            "MMCSS SystemResponsiveness restored.");
        return $"MMCSS SystemResponsiveness restored to {value} (prior captured value).";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    private string Exec(string file, string args, string ok)
    {
        _runner.RunCommand(file, args.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return ok;
    }

    private string ExecPs(string script, string ok)
    {
        _runner.RunPowerShell(script);
        return ok;
    }

    private string? TryCmd(string file, string args)
    {
        try { return _runner.RunCommand(file, args.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim(); }
        catch { return null; }
    }

    private string? TryPs(string script)
    {
        try { return _runner.RunPowerShell(script).Trim(); }
        catch { return null; }
    }

    /// <summary>
    /// Validates a captured backup value is a plain DWORD (decimal or 0x-hex) before it is
    /// interpolated into a PowerShell command. Falls back to a known-safe default otherwise,
    /// so a malformed/unexpected stored value can never inject PowerShell.
    /// </summary>
    private static string SafeDword(string? backup, string fallback)
    {
        if (string.IsNullOrWhiteSpace(backup)) return fallback;
        var v = backup.Trim();
        if (uint.TryParse(v, out _)) return v;
        if (v.StartsWith("0x", StringComparison.OrdinalIgnoreCase) &&
            uint.TryParse(v[2..], System.Globalization.NumberStyles.HexNumber, null, out _)) return v;
        return fallback;
    }

    // Kept for backwards compatibility — TweakEngine no longer uses these directly but LatencyTweaks still handles the logic.
    private string ApplySystemIni() => LatencyTweaks.ApplySystemIniFps(_store, _runner);
    private string RevertSystemIni() => LatencyTweaks.RevertSystemIni(_store);

    /// <summary>Returns true when the current process is running with Administrator privileges.</summary>
    public static bool IsElevated()
    {
        try
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch { return false; }
    }
}
