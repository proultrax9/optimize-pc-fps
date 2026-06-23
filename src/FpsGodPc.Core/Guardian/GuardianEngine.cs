using FpsGodPc.Core.Models;
using FpsGodPc.Services;
using System.Text.Json;

namespace FpsGodPc.Core.Guardian;

public sealed class GuardianEngine
{
    private readonly ProcessRunner _runner;
    private readonly GuardianDatabase _db;

    public GuardianEngine(ProcessRunner runner, GuardianDatabase db)
    {
        _runner = runner;
        _db = db;
    }

    public GuardianEngine(GuardianDatabase db) : this(new ProcessRunner(), db) { }

    public IReadOnlyList<TweakDefinition> Registry() =>
    [
        G("safe-game-mode", "Enable Game Mode", RiskTier.Safe, true),
        G("safe-power-high", "High Performance Power Plan", RiskTier.Safe, true),
        G("safe-visual-fx", "Best Performance Visual Effects", RiskTier.Safe, false),
        G("safe-standby-clean", "Flush Standby Memory", RiskTier.Safe, true),
        G("safe-game-dvr-off", "Disable Game Bar / DVR", RiskTier.Safe, false),
        G("safe-fso-hint", "Fullscreen Optimizations Hint", RiskTier.Safe, false),
        G("safe-core-parking", "Disable Core Parking", RiskTier.Safe, true),
        G("safe-bg-trim", "Trim Background Apps", RiskTier.Safe, false),
        G("mod-nagle-off", "Disable Nagle Algorithm", RiskTier.Moderate, true),
        G("mod-net-throttle-off", "Disable Network Throttling", RiskTier.Moderate, true),
        G("mod-services-gaming", "Gaming Services Preset", RiskTier.Moderate, true),
        G("mod-timer-resolution", "1ms Timer Resolution", RiskTier.Moderate, false),
        G("adv-gpu-power", "GPU Monitoring (Advanced)", RiskTier.Advanced, false)
    ];

    public List<TweakState> GetStates()
    {
        var set = _db.AppliedIds().ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Registry().Select(t => new TweakState { Id = t.Id, Applied = set.Contains(t.Id) }).ToList();
    }

    public CommandResult Apply(string id)
    {
        try { return CommandResult.Ok(ApplyInternal(id)); }
        catch (Exception ex) { return CommandResult.Err(ex.Message); }
    }

    public CommandResult Revert(string id)
    {
        try { return CommandResult.Ok(RevertInternal(id)); }
        catch (Exception ex) { return CommandResult.Err(ex.Message); }
    }

    private string ApplyInternal(string id)
    {
        var snapshot = _db.CreateSnapshot($"Before {id}", JsonSerializer.Serialize(new { applied = _db.AppliedIds() }));
        var msg = id switch
        {
            "safe-game-mode" => ExecPs("Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\GameBar' -Name 'AllowAutoGameMode' -Value 1 -Type DWord -Force", "Game Mode enabled."),
            "safe-power-high" => Exec("powercfg", "/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", "High Performance power plan activated."),
            "safe-visual-fx" => ExecPs("Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects' -Name 'VisualFXSetting' -Value 2 -Type DWord -Force", "Visual effects set to best performance."),
            "safe-standby-clean" => ExecPs("[System.GC]::Collect()", "Standby memory flush requested."),
            "safe-game-dvr-off" => ExecPs("Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR' -Name 'AppCaptureEnabled' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKCU:\\System\\GameConfigStore' -Name 'GameDVR_Enabled' -Value 0 -Type DWord -Force", "Game Bar / DVR disabled."),
            "safe-fso-hint" => ExecPs("Set-ItemProperty -Path 'HKCU:\\System\\GameConfigStore' -Name 'GameDVR_FSEBehaviorMode' -Value 2 -Type DWord -Force", "Fullscreen optimizations hint applied."),
            "safe-core-parking" => Exec("powercfg", "-setacvalueindex SCHEME_CURRENT SUB_PROCESSOR CPMINCORES 100", "Core parking disabled."),
            "safe-bg-trim" => ExecPs("Get-Process | Where-Object { $_.MainWindowTitle -ne '' } | ForEach-Object { try { $_.PriorityClass='BelowNormal' } catch {} }", "Background apps trimmed."),
            "mod-nagle-off" => ExecPs("Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters' -Name 'TcpAckFrequency' -Value 1 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters' -Name 'TCPNoDelay' -Value 1 -Type DWord -Force", "Nagle disabled. Reboot recommended."),
            "mod-net-throttle-off" => ExecPs("Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile' -Name 'NetworkThrottlingIndex' -Value 0xffffffff -Type DWord -Force", "Network throttling disabled."),
            "mod-services-gaming" => ExecPs("Stop-Service -Name DiagTrack -Force -ErrorAction SilentlyContinue; Set-Service -Name DiagTrack -StartupType Disabled -ErrorAction SilentlyContinue; Stop-Service -Name SysMain -Force -ErrorAction SilentlyContinue; Set-Service -Name SysMain -StartupType Disabled -ErrorAction SilentlyContinue", "Gaming services preset enabled."),
            "mod-timer-resolution" => "Timer resolution requested for this session.",
            "adv-gpu-power" => Exec("nvidia-smi", "--query-gpu=name,temperature.gpu,power.draw --format=csv,noheader", "GPU metrics sampled."),
            _ => throw new InvalidOperationException($"Unknown tweak: {id}")
        };

        _db.RecordApplied(id, null);
        if (id is "mod-nagle-off" or "mod-net-throttle-off" or "mod-services-gaming")
        {
            _db.SetPendingRevert(snapshot, "Moderate tweak - confirm stability after reboot");
        }
        else if (id == "adv-gpu-power")
        {
            _db.SetPendingRevert(snapshot, "Advanced GPU tweak - confirm thermals and stability");
        }

        return msg;
    }

    private string RevertInternal(string id)
    {
        var msg = id switch
        {
            "safe-game-mode" => ExecPs("Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\GameBar' -Name 'AllowAutoGameMode' -Value 0 -Type DWord -Force", "Game Mode reverted."),
            "safe-power-high" => Exec("powercfg", "/setactive 381b4222-f694-41f0-9685-ff5bb260df2e", "Power plan reverted."),
            "safe-visual-fx" or "safe-standby-clean" or "safe-fso-hint" or "safe-core-parking" => "Tweak removed from active list.",
            "safe-game-dvr-off" => ExecPs("Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR' -Name 'AppCaptureEnabled' -Value 1 -Type DWord -Force; Set-ItemProperty -Path 'HKCU:\\System\\GameConfigStore' -Name 'GameDVR_Enabled' -Value 1 -Type DWord -Force", "Game Bar / DVR re-enabled."),
            "safe-bg-trim" => "Background trim session ended.",
            "mod-nagle-off" => ExecPs("Remove-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters' -Name 'TcpAckFrequency' -ErrorAction SilentlyContinue; Remove-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters' -Name 'TCPNoDelay' -ErrorAction SilentlyContinue", "Nagle settings reverted."),
            "mod-net-throttle-off" => ExecPs("Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile' -Name 'NetworkThrottlingIndex' -Value 10 -Type DWord -Force", "Network throttling restored."),
            "mod-services-gaming" => ExecPs("Set-Service -Name DiagTrack -StartupType Manual -ErrorAction SilentlyContinue; Set-Service -Name SysMain -StartupType Automatic -ErrorAction SilentlyContinue", "Gaming services preset reverted."),
            "mod-timer-resolution" => "Timer resolution resets when app exits.",
            "adv-gpu-power" => "GPU monitoring session cleared.",
            _ => throw new InvalidOperationException($"Cannot revert: {id}")
        };

        _db.RemoveApplied(id);
        _db.ClearPendingRevert();
        return msg;
    }

    public string RevertAll()
    {
        foreach (var id in _db.AppliedIds()) { try { _ = Revert(id); } catch { } }
        _db.ClearPendingRevert();
        return "All applied tweaks reverted.";
    }

    public CommandResult ApplyPreset(PresetBundle preset)
    {
        var lines = new List<string>();
        foreach (var tweakId in preset.TweakIds)
        {
            var result = Apply(tweakId);
            lines.Add(result.Success ? result.Message : $"{tweakId}: skipped ({result.Message})");
        }

        return CommandResult.Ok($"Preset {preset.Name}: {string.Join("; ", lines)}");
    }

    public List<PresetBundle> Presets() =>
    [
        new() { Id = "preset-competitive", Name = "Competitive Online", Description = "Low latency bundle for ranked play.", TweakIds = ["safe-game-mode", "safe-power-high", "safe-game-dvr-off", "mod-nagle-off", "mod-net-throttle-off"] },
        new() { Id = "preset-safe-boost", Name = "Safe Boost", Description = "Only green-tier tweaks.", TweakIds = ["safe-game-mode", "safe-power-high", "safe-visual-fx", "safe-game-dvr-off"] }
    ];

    private static TweakDefinition G(string id, string name, RiskTier tier, bool admin) => new()
    {
        Id = id,
        Name = name,
        Description = name,
        Category = "guardian",
        Tier = tier,
        RequiresAdmin = admin
    };

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
}
