using FpsGodPc.Core.Models;
using FpsGodPc.Services;

namespace FpsGodPc.Core.Tweaks;

public sealed class TweakEngine
{
    private readonly ProcessRunner _runner;
    private readonly AppStateStore _store;

    public TweakEngine(ProcessRunner runner, AppStateStore store)
    {
        _runner = runner;
        _store = store;
    }

    public TweakEngine(AppStateStore store) : this(new ProcessRunner(), store) { }

    public bool IsAdvisorOnly(string id) =>
        id is "gpu-hags-advisor" or "cpu-undervolt" or "adv-vbs-warn" or "adv-bios-xmp"
            or "gpu-power-limit" or "gpu-clock-offset" or "cpu-power-limit" or "adv-fan-curve"
            or "adv-vbs-disable";

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

    private string ApplyInternal(string id)
    {
        if (IsAdvisorOnly(id))
        {
            _store.MarkApplied(id, null);
            return "Advisor guide - no system changes applied.";
        }

        if (_store.IsApplied(id))
        {
            return "Already applied.";
        }

        var backup = id switch
        {
            "win-game-mode" => TryPs("(Get-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\GameBar' -Name 'AutoGameModeEnabled' -ErrorAction SilentlyContinue).AutoGameModeEnabled"),
            "win-power-high" => TryCmd("powercfg", "/getactivescheme"),
            "win-visual-fx" => TryPs("(Get-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects' -Name 'VisualFXSetting' -ErrorAction SilentlyContinue).VisualFXSetting"),
            _ => null
        };

        var msg = id switch
        {
            "win-game-mode" => ExecPs("Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\GameBar' -Name 'AutoGameModeEnabled' -Value 1 -Type DWord -Force", "Game Mode enabled."),
            "win-power-high" => Exec("powercfg", "/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", "High Performance power plan activated."),
            "win-visual-fx" => ExecPs("Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects' -Name 'VisualFXSetting' -Value 2 -Type DWord -Force -ErrorAction SilentlyContinue", "Visual effects set to best performance."),
            "win-game-dvr" => ExecPs("Set-ItemProperty -Path 'HKCU:\\System\\GameConfigStore' -Name 'GameDVR_Enabled' -Value 0 -Type DWord -Force -ErrorAction SilentlyContinue; Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR' -Name 'AppCaptureEnabled' -Value 0 -Type DWord -Force -ErrorAction SilentlyContinue", "Xbox Game Bar DVR disabled."),
            "win-telemetry" => ExecPs("Stop-Service -Name DiagTrack -Force -ErrorAction SilentlyContinue; Set-Service -Name DiagTrack -StartupType Disabled -ErrorAction SilentlyContinue; Stop-Service -Name dmwappushservice -Force -ErrorAction SilentlyContinue; Set-Service -Name dmwappushservice -StartupType Disabled -ErrorAction SilentlyContinue", "Telemetry services disabled (requires admin)."),
            "win-fullscreen-opt" => ExecPs("Set-ItemProperty -Path 'HKCU:\\System\\GameConfigStore' -Name 'GameDVR_DXGIHonorFSEWindowsCompatible' -Value 1 -Type DWord -Force -ErrorAction SilentlyContinue; Set-ItemProperty -Path 'HKCU:\\System\\GameConfigStore' -Name 'GameDVR_FSEBehaviorMode' -Value 2 -Type DWord -Force -ErrorAction SilentlyContinue", "Fullscreen optimizations disabled globally."),
            "win-bg-apps" => ExecPs("$path='HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications'; if (Test-Path $path) { Get-ChildItem $path | ForEach-Object { Set-ItemProperty -Path $_.PSPath -Name 'Disabled' -Value 1 -Type DWord -Force -ErrorAction SilentlyContinue } }", "Background apps limited."),
            "gpu-shader-cache" => ExecPs("$paths=@('$env:LOCALAPPDATA\\D3DSCache','$env:LOCALAPPDATA\\NVIDIA\\DXCache','$env:LOCALAPPDATA\\AMD\\DxCache'); foreach ($p in $paths) { if (Test-Path $p) { Remove-Item $p\\* -Recurse -Force -ErrorAction SilentlyContinue } }", "DirectX / GPU shader cache cleared."),
            "gpu-max-perf" => ExecPs("$nv='HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000'; if (Test-Path $nv) { Set-ItemProperty -Path $nv -Name 'PerfLevelSrc' -Value 0x2222 -Type DWord -Force -ErrorAction SilentlyContinue }", "GPU set to prefer maximum performance (NVIDIA path)."),
            "gpu-low-latency" => ExecPs("$nv='HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000'; if (Test-Path $nv) { Set-ItemProperty -Path $nv -Name 'RMHdcpKeyglobZero' -Value 1 -Type DWord -Force -ErrorAction SilentlyContinue }", "Low latency mode hint applied where supported."),
            "cpu-game-priority" => "Game priority is applied at runtime when a game is detected (session-based).",
            "cpu-core-parking" => Exec("powercfg", "-setacvalueindex SCHEME_CURRENT 54533251-82be-4824-96c1-47b60b740d00 0cc5b647-c1df-4637-891a-dec35c318583 0", "CPU core parking disabled."),
            "cpu-timer-res" => "Timer resolution will be requested during active game sessions.",
            "net-dns-flush" => Exec("ipconfig", "/flushdns", "DNS cache flushed."),
            "net-adapter-power" => ExecPs("Get-NetAdapter -Physical | ForEach-Object { Disable-NetAdapterPowerManagement -Name $_.Name -ErrorAction SilentlyContinue }", "Network adapter power saving disabled."),
            "net-nagle" => ExecPs("Get-ChildItem 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces' | ForEach-Object { Set-ItemProperty -Path $_.PSPath -Name 'TcpAckFrequency' -Value 1 -Type DWord -Force -ErrorAction SilentlyContinue; Set-ItemProperty -Path $_.PSPath -Name 'TCPNoDelay' -Value 1 -Type DWord -Force -ErrorAction SilentlyContinue }", "Nagle's algorithm disabled on network interfaces."),
            "net-throttling" => ExecPs("Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile' -Name 'NetworkThrottlingIndex' -Value 0xffffffff -Type DWord -Force", "Network throttling index tuned."),
            "adv-ram-standby" => ExecPs("$sig='[DllImport(\"\"psapi.dll\"\")] public static extern int EmptyWorkingSet(IntPtr hwProcess);'; Add-Type -MemberDefinition $sig -Name PSApi -Namespace Win32 -ErrorAction SilentlyContinue; [Win32.PSApi]::EmptyWorkingSet([System.Diagnostics.Process]::GetCurrentProcess().Handle) | Out-Null", "Standby memory trimmed."),
            "win-priority-26" => Exec("reg", @"add HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl /v Win32PrioritySeparation /t REG_DWORD /d 0x26 /f", "Win32PrioritySeparation set to 0x26 (gaming latency profile)."),
            "win-mmcss-latency" => ExecPs("Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile' -Name 'SystemResponsiveness' -Value 0 -Type DWord -Force", "MMCSS gaming profile applied."),
            "win-system-ini-fps" => ApplySystemIni(),
            "win-disable-power-saving" => Exec("powercfg", "/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", "Power saving reduced (best effort)."),
            _ => throw new InvalidOperationException($"Tweak '{id}' is not implemented yet.")
        };

        _store.MarkApplied(id, backup);
        return msg;
    }

    private string RevertInternal(string id)
    {
        if (IsAdvisorOnly(id))
        {
            _store.MarkReverted(id);
            return "Advisor - nothing to revert.";
        }
        if (!_store.IsApplied(id)) return "Tweak was not applied.";

        var backup = _store.GetBackup(id);
        var msg = id switch
        {
            "win-game-mode" => ExecPs($"Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\GameBar' -Name 'AutoGameModeEnabled' -Value {backup ?? "0"} -Type DWord -Force", "Game Mode reverted."),
            "win-power-high" => Exec("powercfg", "/setactive 381b4222-f694-41f0-9685-ff5bb260df2e", "Power plan restored to Balanced."),
            "win-visual-fx" => ExecPs($"Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects' -Name 'VisualFXSetting' -Value {backup ?? "0"} -Type DWord -Force -ErrorAction SilentlyContinue", "Visual effects restored."),
            "win-game-dvr" => ExecPs("Set-ItemProperty -Path 'HKCU:\\System\\GameConfigStore' -Name 'GameDVR_Enabled' -Value 1 -Type DWord -Force -ErrorAction SilentlyContinue; Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR' -Name 'AppCaptureEnabled' -Value 1 -Type DWord -Force -ErrorAction SilentlyContinue", "Game DVR re-enabled."),
            "win-telemetry" => ExecPs("Set-Service -Name DiagTrack -StartupType Manual -ErrorAction SilentlyContinue; Set-Service -Name dmwappushservice -StartupType Manual -ErrorAction SilentlyContinue", "Telemetry services restored to manual."),
            "win-fullscreen-opt" => ExecPs("Remove-ItemProperty -Path 'HKCU:\\System\\GameConfigStore' -Name 'GameDVR_DXGIHonorFSEWindowsCompatible' -ErrorAction SilentlyContinue; Remove-ItemProperty -Path 'HKCU:\\System\\GameConfigStore' -Name 'GameDVR_FSEBehaviorMode' -ErrorAction SilentlyContinue", "Fullscreen optimizations restored."),
            "win-bg-apps" or "gpu-shader-cache" or "cpu-game-priority" or "cpu-timer-res" => "Reverted (no persistent change or cache already cleared).",
            "gpu-max-perf" or "gpu-low-latency" => "GPU driver settings revert - use NVIDIA/AMD control panel defaults.",
            "cpu-core-parking" => Exec("powercfg", "-setacvalueindex SCHEME_CURRENT 54533251-82be-4824-96c1-47b60b740d00 0cc5b647-c1df-4637-891a-dec35c318583 100", "Core parking restored."),
            "net-dns-flush" => "DNS flush is one-way - no revert needed.",
            "net-adapter-power" => ExecPs("Get-NetAdapter -Physical | ForEach-Object { Enable-NetAdapterPowerManagement -Name $_.Name -ErrorAction SilentlyContinue }", "Adapter power management re-enabled."),
            "net-nagle" => ExecPs("Get-ChildItem 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces' | ForEach-Object { Remove-ItemProperty -Path $_.PSPath -Name 'TcpAckFrequency' -ErrorAction SilentlyContinue; Remove-ItemProperty -Path $_.PSPath -Name 'TCPNoDelay' -ErrorAction SilentlyContinue }", "Network latency tweaks reverted."),
            "net-throttling" => ExecPs("Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile' -Name 'NetworkThrottlingIndex' -Value 10 -Type DWord -Force", "Network throttling restored."),
            "adv-ram-standby" => "Standby cleaner is one-shot - no revert.",
            "win-priority-26" => Exec("reg", @"add HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl /v Win32PrioritySeparation /t REG_DWORD /d 0x2 /f", "Win32PrioritySeparation restored."),
            "win-mmcss-latency" => ExecPs("Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile' -Name 'SystemResponsiveness' -Value 20 -Type DWord -Force", "MMCSS SystemResponsiveness restored."),
            "win-system-ini-fps" => RevertSystemIni(),
            "win-disable-power-saving" => Exec("powercfg", "/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", "High Performance plan re-applied."),
            _ => throw new InvalidOperationException($"Revert for '{id}' not implemented.")
        };

        _store.MarkReverted(id);
        return msg;
    }

    private string Exec(string file, string args, string ok) { _runner.RunCommand(file, args.Split(' ', StringSplitOptions.RemoveEmptyEntries)); return ok; }
    private string ExecPs(string script, string ok) { _runner.RunPowerShell(script); return ok; }
    private string? TryCmd(string file, string args) { try { return _runner.RunCommand(file, args.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim(); } catch { return null; } }
    private string? TryPs(string script) { try { return _runner.RunPowerShell(script).Trim(); } catch { return null; } }

    private string ApplySystemIni()
    {
        var windir = Environment.GetEnvironmentVariable("WINDIR") ?? @"C:\Windows";
        var iniPath = Path.Combine(windir, "system.ini");
        var backupDir = Path.Combine(_store.DataDirectory, "backups");
        Directory.CreateDirectory(backupDir);
        var backupPath = Path.Combine(backupDir, "system.ini.bak");
        if (File.Exists(iniPath) && !File.Exists(backupPath)) File.Copy(iniPath, backupPath, false);
        File.WriteAllText(iniPath, "[386Enh]\nMinTimeSlice=1\nAvgTimeSlice=1\nMaxTimeSlice=1\n");
        return "system.ini latency profile applied (backup saved).";
    }

    private string RevertSystemIni()
    {
        var windir = Environment.GetEnvironmentVariable("WINDIR") ?? @"C:\Windows";
        var iniPath = Path.Combine(windir, "system.ini");
        var backupPath = Path.Combine(_store.DataDirectory, "backups", "system.ini.bak");
        if (!File.Exists(backupPath)) throw new InvalidOperationException("No system.ini backup found.");
        File.Copy(backupPath, iniPath, true);
        return "system.ini restored from backup.";
    }
}
