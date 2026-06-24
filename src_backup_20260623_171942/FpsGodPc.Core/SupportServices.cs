using FpsGodPc.Core.Models;
using FpsGodPc.Core.Tweaks;
using FpsGodPc.Services;

namespace FpsGodPc.Core;

public sealed class ScannerService
{
    private readonly SystemInfoService _systemInfo;

    public ScannerService(SystemInfoService systemInfo) => _systemInfo = systemInfo;

    public ScanResult RunScan()
    {
        var info = _systemInfo.Collect(TweakCatalog.ApplicableCount);
        var findings = new List<ScanFinding>();
        var issues = 0u;

        findings.Add(Finding("game-mode", "windows", "Game Mode",
            info.GameModeEnabled ? "ok" : "warn",
            info.GameModeEnabled ? "Game Mode is enabled." : "Game Mode is disabled.",
            info.GameModeEnabled ? "No action needed." : "Enable Game Mode in Tweaks.",
            ref issues, !info.GameModeEnabled));

        var powerOk = info.PowerPlan.Contains("high", StringComparison.OrdinalIgnoreCase)
            || info.PowerPlan.Contains("ultimate", StringComparison.OrdinalIgnoreCase);
        findings.Add(Finding("power-plan", "power", "Power Plan",
            powerOk ? "ok" : "warn",
            $"Active plan: {info.PowerPlan}",
            powerOk ? "Power plan is performance-oriented." : "Switch to High Performance in Tweaks.",
            ref issues, !powerOk));

        var mem = double.TryParse(info.MemoryTotalGb, out var gb) ? gb : 0;
        findings.Add(Finding("memory", "hardware", "System Memory",
            mem >= 16 ? "ok" : "warn",
            $"{info.MemoryTotalGb} GB",
            mem >= 16 ? "Sufficient for competitive gaming." : "16GB+ recommended.",
            ref issues, mem < 16));

        findings.Add(Finding("gpu", "hardware", "Graphics",
            info.GpuName != "—" ? "ok" : "warn",
            $"{info.GpuName} · {info.GpuVramGb} GB VRAM",
            info.GpuName != "—" ? "GPU detected." : "GPU not detected — update drivers.",
            ref issues, info.GpuName == "—"));

        var score = info.PerformanceScore;
        return new ScanResult
        {
            Findings = findings,
            FpsGain = issues == 0 ? "+0–5% (already tuned)" : $"+{Math.Min(issues * 3, 15)}–{Math.Min(issues * 5, 25)}% potential",
            LatencyGain = issues >= 2 ? "Moderate latency gains available" : "Low latency profile possible",
            StabilityRisk = issues >= 4 ? "Medium" : "Low",
            RecommendedMode = issues >= 3 ? "competitive" : issues >= 1 ? "safe" : "maintain",
            PerformanceScore = score,
        };
    }

    private static ScanFinding Finding(string id, string cat, string title, string status, string detail, string rec, ref uint issues, bool count)
    {
        if (count) issues++;
        return new ScanFinding { Id = id, Category = cat, Title = title, Status = status, Detail = detail, Recommendation = rec };
    }
}

public sealed class CleanerService
{
    private readonly ProcessRunner _runner;
    public CleanerService(ProcessRunner runner) => _runner = runner;

    public CleanResult Run(CleanOptions opts)
    {
        var items = new List<string>();
        ulong freed = 0;

        if (opts.TempFiles)
        {
            try
            {
                var outText = _runner.RunPowerShell("""
                    $paths = @($env:TEMP, "$env:LOCALAPPDATA\Temp", "$env:WINDIR\Temp")
                    $freed = 0
                    foreach ($p in $paths) {
                        if (Test-Path $p) {
                            Get-ChildItem $p -ErrorAction SilentlyContinue | ForEach-Object {
                                try { $freed += $_.Length; Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue } catch {}
                            }
                        }
                    }
                    Write-Output $freed
                    """);
                if (ulong.TryParse(outText.Trim(), out var n)) freed += n;
                items.Add("Temporary files");
            }
            catch { }
        }

        if (opts.ShaderCache)
        {
            try
            {
                _runner.RunPowerShell("""
                    $paths = @("$env:LOCALAPPDATA\D3DSCache", "$env:LOCALAPPDATA\NVIDIA\DXCache")
                    foreach ($p in $paths) { if (Test-Path $p) { Remove-Item $p\* -Recurse -Force -ErrorAction SilentlyContinue } }
                    """);
                items.Add("Shader cache");
            }
            catch { }
        }

        if (opts.DnsCache)
        {
            try { _runner.RunCommand("ipconfig", "/flushdns"); items.Add("DNS cache"); }
            catch { }
        }

        if (opts.RecycleBin)
        {
            try { _runner.RunPowerShell("Clear-RecycleBin -Force -ErrorAction SilentlyContinue"); items.Add("Recycle bin"); }
            catch { }
        }

        return new CleanResult
        {
            FreedMb = freed / 1024.0 / 1024.0,
            Items = items,
            Message = items.Count == 0 ? "No cleanup options selected." : $"Cleaned {items.Count} item(s).",
        };
    }
}

public sealed class RestoreService
{
    private readonly ProcessRunner _runner;
    private readonly AppStateStore _store;
    private readonly Tweaks.TweakEngine _tweaks;

    public RestoreService(ProcessRunner runner, AppStateStore store, Tweaks.TweakEngine tweaks)
    {
        _runner = runner;
        _store = store;
        _tweaks = tweaks;
    }

    public List<RestorePoint> ListRestorePoints()
    {
        try
        {
            var raw = _runner.RunPowerShell("""
                Get-ComputerRestorePoint -ErrorAction SilentlyContinue |
                  Select-Object SequenceNumber, Description, CreationTime |
                  ConvertTo-Json -Compress
                """);
            if (string.IsNullOrWhiteSpace(raw)) return [];
            if (raw.TrimStart().StartsWith('['))
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<RestorePointDto>>(raw)?
                    .Select(d => new RestorePoint
                    {
                        SequenceNumber = d.SequenceNumber,
                        Description = d.Description ?? "",
                        CreationTime = d.CreationTime ?? "",
                    }).ToList() ?? [];
            }

            var one = System.Text.Json.JsonSerializer.Deserialize<RestorePointDto>(raw);
            return one is null ? [] : [new RestorePoint { SequenceNumber = one.SequenceNumber, Description = one.Description ?? "", CreationTime = one.CreationTime ?? "" }];
        }
        catch
        {
            return [];
        }
    }

    public CommandResult CreateRestorePoint(string description)
    {
        try
        {
            var escaped = description.Replace("'", "''");
            _runner.RunPowerShell($"Checkpoint-Computer -Description '{escaped}' -RestorePointType 'MODIFY_SETTINGS' -ErrorAction Stop");
            return CommandResult.Ok("Restore point created.");
        }
        catch (Exception ex)
        {
            return CommandResult.Err(ex.Message);
        }
    }

    public RollbackInfo GetRollbackInfo()
    {
        var (entries, lastBoost, lastBoostAt) = _store.RollbackInfo();
        return new RollbackInfo
        {
            Entries = entries.Select(e => new RollbackEntry { TweakId = e.Id, AppliedAt = e.AppliedAt }).ToList(),
            LastBoost = lastBoost,
            LastBoostAt = lastBoostAt,
        };
    }

    public CommandResult RollbackAll() => _tweaks.RollbackAll();

    private sealed class RestorePointDto
    {
        public uint SequenceNumber { get; set; }
        public string? Description { get; set; }
        public string? CreationTime { get; set; }
    }
}

public sealed class NetworkService
{
    private readonly ProcessRunner _runner;
    public NetworkService(ProcessRunner runner) => _runner = runner;

    public PingResult Ping(string host)
    {
        try
        {
            var raw = _runner.RunCommand("ping", "-n", "4", host);
            double? latency = null;
            foreach (var line in raw.Split('\n'))
            {
                if (line.Contains("Average", StringComparison.OrdinalIgnoreCase) || line.Contains("เฉลี่ย"))
                {
                    var parts = line.Split('=');
                    if (parts.Length > 1)
                    {
                        var ms = parts[^1].Replace("ms", "").Trim();
                        if (double.TryParse(ms, out var v)) latency = v;
                    }
                }
            }

            return new PingResult { Host = host, LatencyMs = latency, Message = raw };
        }
        catch (Exception ex)
        {
            return new PingResult { Host = host, Message = ex.Message };
        }
    }
}

public sealed class GameProfilesService
{
    public List<GameProfile> ListBuiltIn() =>
    [
        new() { Id = "valorant", Name = "Valorant", Executable = "VALORANT-Win64-Shipping.exe", FpsCap = 0, Priority = "High", LaunchOptions = "-nosplash", Notes = ["Competitive preset friendly"] },
        new() { Id = "cs2", Name = "Counter-Strike 2", Executable = "cs2.exe", FpsCap = 0, Priority = "High", LaunchOptions = "-novid", Notes = ["Low latency network tweaks pair well"] },
        new() { Id = "fortnite", Name = "Fortnite", Executable = "FortniteClient-Win64-Shipping.exe", FpsCap = 0, Priority = "AboveNormal", LaunchOptions = "", Notes = ["Performance mode recommended in-game"] },
    ];
}
