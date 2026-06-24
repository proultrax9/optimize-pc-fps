using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.Json;
using FpsGodPc.Core.Guardian;
using FpsGodPc.Core.Localization;
using FpsGodPc.Core.Models;
using FpsGodPc.Core;
using FpsGodPc.Core.Tweaks;
using FpsGodPc.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FpsGodPc.Core.Services;

public sealed class AppServices
{
    private readonly ProcessRunner _processRunner;
    private readonly AppStateStore _stateStore;
    private readonly GuardianDatabase _guardianDatabase;
    private readonly SystemInfoService _systemInfoService;
    private readonly TelemetryService _telemetryService;
    private readonly ElevationHelper _elevationHelper;
    private readonly TweakEngine _tweakEngine;
    private readonly BoostService _boostService;
    private readonly GuardianEngine _guardianEngine;
    private readonly SafetyService _safetyService;
    private readonly RestoreService _restoreService;
    private readonly CleanerService _cleanerService;
    private readonly PresentMonService _presentMonService;
    private readonly GameWatcherService _gameWatcherService;
    private readonly GameInstallDetector _gameInstallDetector;
    private readonly PowerPlanService _powerPlanService;
    private readonly UnigineBenchmarkService _unigineBenchmarkService;
    private readonly SpaceBattleBenchmarkService _spaceBattleBenchmarkService;
    private readonly LocalizationService _l10n;

    public AppServices(
        ProcessRunner processRunner,
        AppStateStore stateStore,
        GuardianDatabase guardianDatabase,
        SystemInfoService systemInfoService,
        TelemetryService telemetryService,
        ElevationHelper elevationHelper,
        TweakEngine tweakEngine,
        BoostService boostService,
        GuardianEngine guardianEngine,
        SafetyService safetyService,
        RestoreService restoreService,
        CleanerService cleanerService,
        PresentMonService presentMonService,
        GameWatcherService gameWatcherService,
        GameInstallDetector gameInstallDetector,
        PowerPlanService powerPlanService,
        UnigineBenchmarkService unigineBenchmarkService,
        SpaceBattleBenchmarkService spaceBattleBenchmarkService,
        LocalizationService l10n)
    {
        _processRunner = processRunner;
        _stateStore = stateStore;
        _guardianDatabase = guardianDatabase;
        _systemInfoService = systemInfoService;
        _telemetryService = telemetryService;
        _elevationHelper = elevationHelper;
        _tweakEngine = tweakEngine;
        _boostService = boostService;
        _guardianEngine = guardianEngine;
        _safetyService = safetyService;
        _restoreService = restoreService;
        _cleanerService = cleanerService;
        _presentMonService = presentMonService;
        _gameWatcherService = gameWatcherService;
        _gameInstallDetector = gameInstallDetector;
        _powerPlanService = powerPlanService;
        _unigineBenchmarkService = unigineBenchmarkService;
        _spaceBattleBenchmarkService = spaceBattleBenchmarkService;
        _l10n = l10n;
        _l10n.SetLanguage(_stateStore.GetSettings().Language);
    }

    private CommandResult Localize(CommandResult result) =>
        result.Success
            ? CommandResult.Ok(_l10n.LocalizeResult(result.Message))
            : CommandResult.Err(_l10n.LocalizeResult(result.Message));

    public ApplyBoostResult LocalizeBoost(ApplyBoostResult result) => new()
    {
        Applied = result.Applied,
        Failed = result.Failed,
        Message = _l10n.LocalizeResult(result.Message),
    };

    public void StartBackgroundServices() => _gameWatcherService.Start();

    public void MarkSessionStart() => _guardianDatabase.MarkSessionStart();

    public void MarkSessionCleanExit() => _guardianDatabase.MarkSessionCleanExit();

    public bool IsElevated() => _elevationHelper.IsElevated();

    public void RestartElevated() => _elevationHelper.RestartElevated();

    public string? GetCrashWatchdogMessage() =>
        _guardianDatabase.ShouldShowCrashWatchdog() ? _l10n.WatchdogMessage() : null;

    public SystemInfoSnapshot CollectSystemInfo() => _systemInfoService.Collect(TweakCatalog.ApplicableCount);

    public TelemetrySnapshot CollectTelemetry() => _telemetryService.Collect();

    public IReadOnlyList<TweakDefinition> GetTweaksByCategory(string category) =>
        TweakCatalog.All.Where(t => string.Equals(t.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();

    public IReadOnlyList<string> GetCategories() =>
        TweakCatalog.All.Select(t => t.Category).Distinct().OrderBy(c => c).ToList();

    public bool IsTweakApplied(string tweakId) => _stateStore.IsApplied(tweakId);

    public CommandResult SetTweakState(string tweakId, bool enabled)
    {
        var tweak = TweakCatalog.Get(tweakId);
        if (tweak is null)
        {
            return Localize(CommandResult.Err($"Unknown tweak: {tweakId}"));
        }

        if (enabled)
        {
            var result = _tweakEngine.ApplyTweak(tweakId);
            if (result.Success)
            {
                _guardianDatabase.RecordApplied(tweakId, null);
            }

            return Localize(result);
        }

        var revert = _tweakEngine.RevertTweak(tweakId);
        if (revert.Success)
        {
            _guardianDatabase.RemoveApplied(tweakId);
        }

        return Localize(revert);
    }

    public IReadOnlyList<PresetBundle> GetBoostPresets() =>
    [
        BuildBoostPreset("safe"),
        BuildBoostPreset("competitive"),
        BuildBoostPreset("extreme"),
        BuildBoostPreset("expert", advisorOnly: true),
    ];

    private PresetBundle BuildBoostPreset(string id, bool advisorOnly = false)
    {
        var tweakIds = BoostService.GetPresetIds(id).ToList();
        var tweakNames = tweakIds.Select(tid => _l10n.TweakName(tid)).ToList();
        return new PresetBundle
        {
            Id = id,
            Name = _l10n.BoostName(id),
            Description = _l10n.BoostTagline(id),
            RiskLevel = _l10n.BoostRiskLevel(id),
            RiskLabel = _l10n.BoostRiskLabel(id),
            Warning = _l10n.BoostWarning(id),
            TweakIds = tweakIds,
            TweakNames = tweakNames.Take(6).ToList(),
            MoreTweakCount = Math.Max(0, tweakNames.Count - 6),
            IncludesLabel = _l10n.BoostIncludes(tweakNames.Count),
            RequiresRestorePoint = id is "competitive" or "extreme",
            IsAdvisorOnly = advisorOnly,
            ApplyButtonLabel = advisorOnly ? _l10n.BoostViewChecklist : _l10n.BoostApplyBoost,
        };
    }

    public ApplyBoostResult ApplyBoostPreset(string presetId) =>
        LocalizeBoost(_boostService.ApplyBoost(presetId));

    public ScanResult RunScanner()
    {
        var info = CollectSystemInfo();
        var telemetry = CollectTelemetry();
        var findings = new List<ScanFinding>();

        if (!info.GameModeEnabled)
        {
            findings.Add(new ScanFinding
            {
                Id = "game-mode",
                Category = _l10n.Category("windows"),
                Title = _l10n.T("Game Mode disabled", "Game Mode ปิดอยู่"),
                Status = "warn",
                Detail = _l10n.T("Windows Game Mode is currently disabled.", "Windows Game Mode ปิดอยู่"),
                Recommendation = _l10n.T("Enable Game Mode in Tweaks.", "เปิด Game Mode ในหน้า Tweaks")
            });
        }

        var cpuLoad = telemetry.CpuUsagePct;
        if (cpuLoad > 75)
        {
            findings.Add(new ScanFinding
            {
                Id = "cpu-load",
                Category = _l10n.Category("cpu"),
                Title = _l10n.T("High CPU background activity", "CPU พื้นหลังใช้งานสูง"),
                Status = "warn",
                Detail = _l10n.T($"Current CPU usage is {cpuLoad:F0}%.", $"CPU ปัจจุบัน {cpuLoad:F0}%"),
                Recommendation = _l10n.T("Run Cleaner and disable unnecessary startup apps.", "ใช้ Cleaner และปิดแอปที่ไม่จำเป็นตอนเปิดเครื่อง")
            });
        }

        if (findings.Count == 0)
        {
            findings.Add(new ScanFinding
            {
                Id = "healthy",
                Category = _l10n.T("System", "ระบบ"),
                Title = _l10n.T("No critical findings", "ไม่พบปัญหาร้ายแรง"),
                Status = "ok",
                Detail = _l10n.T("System baseline looks healthy for gaming.", "ระบบพร้อมสำหรับเล่นเกม"),
                Recommendation = _l10n.T("Apply Safe or Competitive boost for extra gains.", "ใช้บูสต์ Safe หรือ Competitive เพื่อเพิ่มประสิทธิภาพ")
            });
        }

        var score = Math.Clamp((int)info.PerformanceScore, 0, 100);
        var presetId = score >= 85 ? "safe" : score >= 70 ? "competitive" : "extreme";
        var stabilityKey = presetId == "extreme" ? "High" : presetId == "competitive" ? "Medium" : "Low";

        return new ScanResult
        {
            Findings = findings,
            FpsGain = _l10n.ScanFpsGain(score),
            LatencyGain = _l10n.ScanLatencyGain(score),
            StabilityRisk = _l10n.ScanStabilityRisk(stabilityKey),
            RecommendedMode = _l10n.ScanRecommendedMode(presetId),
            RecommendedPresetId = presetId,
            PerformanceScore = (uint)score,
        };
    }

    public CommandResult RunBenchmark(string label, uint durationSecs)
    {
        durationSecs = Math.Clamp(durationSecs, 10u, 120u);
        var resolvedLabel = string.IsNullOrWhiteSpace(label) ? _l10n.BenchmarkDefaultLabel : label;
        var pm = _presentMonService.GetStatus();

        if (pm.Available && pm.Path is not null)
        {
            var benchmarkDir = Path.Combine(_stateStore.DataDirectory, "benchmark");
            var captured = _presentMonService.TryCapture(resolvedLabel, durationSecs, pm.Path, benchmarkDir);
            if (captured is not null)
            {
                var telemetry = _telemetryService.Collect();
                captured.AvgCpuPct = telemetry.CpuUsagePct;
                captured.AvgGpuPct = telemetry.GpuUsagePct;
                _guardianDatabase.SaveBenchmark(captured);
                return CommandResult.Ok(_l10n.BenchmarkSaved(resolvedLabel, captured.Source));
            }
        }

        var cpuSamples = new List<float>();
        var gpuSamples = new List<float>();
        for (uint i = 0; i < durationSecs; i++)
        {
            var sample = _telemetryService.Collect();
            cpuSamples.Add(sample.CpuUsagePct);
            if (sample.GpuUsagePct is float gpu)
            {
                gpuSamples.Add(gpu);
            }

            Thread.Sleep(1000);
        }

        var fallback = new BenchmarkSession
        {
            Label = resolvedLabel,
            TakenAt = DateTimeOffset.Now.ToString("s"),
            DurationSecs = durationSecs,
            AvgCpuPct = cpuSamples.Count == 0 ? 0 : cpuSamples.Average(),
            AvgGpuPct = gpuSamples.Count == 0 ? null : gpuSamples.Average(),
            Source = "telemetry",
            Notes = _l10n.BenchmarkTelemetryNote,
        };

        _guardianDatabase.SaveBenchmark(fallback);
        return CommandResult.Ok(_l10n.BenchmarkSaved(resolvedLabel, fallback.Source));
    }

    public PresentMonStatus GetPresentMonStatus()
    {
        var status = _presentMonService.GetStatus();
        return new PresentMonStatus
        {
            Available = status.Available,
            Path = status.Path,
            Message = _l10n.LocalizePresentMonStatus(status.Message),
        };
    }

    public WatcherStatus GetWatcherStatus() => _gameWatcherService.GetStatus();

    public IReadOnlyList<BenchmarkSession> ListBenchmarks() => _guardianDatabase.ListBenchmarks();

    public BenchmarkCompareResult? CompareLatestBenchmarks()
    {
        var sessions = _guardianDatabase.ListBenchmarks();
        if (sessions.Count < 2)
        {
            return null;
        }

        var after = sessions[0];
        var before = sessions[1];
        return new BenchmarkCompareResult
        {
            BeforeLabel = before.Label,
            AfterLabel = after.Label,
            BeforeFps = before.AvgFps,
            AfterFps = after.AvgFps,
            BeforePct1Low = before.Pct1Low,
            AfterPct1Low = after.Pct1Low,
            FpsDelta = after.AvgFps is float a && before.AvgFps is float b ? a - b : null,
            Pct1LowDelta = after.Pct1Low is float a1 && before.Pct1Low is float b1 ? a1 - b1 : null,
        };
    }

    public SafetyStatus GetSafetyStatus() => _guardianDatabase.GetSafetyStatus();

    public IReadOnlyList<SnapshotSummary> ListSnapshots() => _guardianDatabase.ListSnapshots();

    public CommandResult CreateSnapshot(string label)
    {
        var json = JsonSerializer.Serialize(_stateStore.AppliedMap());
        var id = _guardianDatabase.CreateSnapshot(label, json);
        return Localize(CommandResult.Ok($"Snapshot #{id} created."));
    }

    public CommandResult RestoreSnapshot(long snapshotId)
    {
        var json = _guardianDatabase.GetSnapshotState(snapshotId);
        var appliedMap = JsonSerializer.Deserialize<Dictionary<string, AppliedRecord>>(json) ?? [];

        foreach (var current in _stateStore.AppliedIds().ToList())
        {
            _stateStore.MarkReverted(current);
            _guardianDatabase.RemoveApplied(current);
        }

        foreach (var pair in appliedMap)
        {
            _stateStore.MarkApplied(pair.Key, pair.Value.Backup);
            _guardianDatabase.RecordApplied(pair.Key, pair.Value.Backup);
        }

        _guardianDatabase.MarkKnownGood(snapshotId);
        _guardianDatabase.ClearPendingRevert();
        return Localize(CommandResult.Ok($"Snapshot #{snapshotId} restored."));
    }

    public CommandResult RollbackAll() => Localize(_tweakEngine.RollbackAll());

    public CommandResult DismissCrashWatchdog()
    {
        _guardianDatabase.DismissCrashWatchdog();
        return Localize(CommandResult.Ok("Crash watchdog dismissed."));
    }

    public RollbackInfo GetRollbackInfo()
    {
        var (entries, lastBoost, lastBoostAt) = _stateStore.RollbackInfo();
        return new RollbackInfo
        {
            LastBoost = lastBoost,
            LastBoostAt = lastBoostAt,
            Entries = entries.Select(e => new RollbackEntry { TweakId = e.Id, AppliedAt = e.AppliedAt }).ToList(),
        };
    }

    public IReadOnlyList<RestorePoint> ListRestorePoints() => _restoreService.ListRestorePoints();

    public CommandResult CreateRestorePoint(string description)
    {
        var result = _restoreService.CreateRestorePoint(description);
        if (!result.Success)
        {
            return CommandResult.Err(_l10n.RestorePointFailed(result.Message));
        }

        return CommandResult.Ok(_l10n.RestorePointCreated());
    }

    public IReadOnlyList<WatcherProfile> ListProfiles()
    {
        var profiles = _guardianDatabase.ListProfiles();
        if (profiles.Count == 0)
        {
            var defaults = BuildDefaultProfiles();
            foreach (var profile in defaults)
            {
                _guardianDatabase.UpsertProfile(profile);
            }

            profiles = _guardianDatabase.ListProfiles();
        }

        var detected = _gameInstallDetector.DetectPaths(profiles.Select(p => p.Executable));
        foreach (var profile in profiles)
        {
            if (detected.TryGetValue(profile.Executable, out var path))
            {
                profile.Installed = true;
                profile.InstallPath = path;
                profile.Executable = path;
            }
            else
            {
                profile.Installed = File.Exists(profile.Executable);
                profile.InstallPath = profile.Installed ? profile.Executable : null;
            }

            profile.Active = profile.TweakIds.Any(_stateStore.IsApplied);
        }

        return profiles;
    }

    public CommandResult SetProfileWatcher(string id, bool enabled)
    {
        _guardianDatabase.SetProfileWatcher(id, enabled);
        return Localize(CommandResult.Ok($"Watcher {(enabled ? "enabled" : "disabled")} for {id}."));
    }

    public CommandResult ApplyProfile(string id)
    {
        var profile = ListProfiles().FirstOrDefault(p => p.Id == id);
        if (profile is null)
        {
            return Localize(CommandResult.Err($"Profile not found: {id}"));
        }

        var applied = 0;
        foreach (var tweakId in profile.TweakIds)
        {
            if (SetTweakState(tweakId, true).Success)
            {
                applied++;
            }
        }

        return CommandResult.Ok(_l10n.LocalizeResult($"Applied {applied} tweaks for {profile.Name}."));
    }

    public CommandResult RevertProfile(string id)
    {
        var profile = ListProfiles().FirstOrDefault(p => p.Id == id);
        if (profile is null)
        {
            return Localize(CommandResult.Err($"Profile not found: {id}"));
        }

        foreach (var tweakId in profile.TweakIds)
        {
            SetTweakState(tweakId, false);
        }

        return CommandResult.Ok(_l10n.LocalizeResult($"Reverted tweaks for {profile.Name}."));
    }

    public IReadOnlyList<GameProfile> GetGameProfiles() =>
        ListProfiles().Select(p => new GameProfile
        {
            Id = p.Id,
            Name = p.Name,
            Executable = p.Executable,
            LaunchOptions = p.LaunchOptions,
            Notes = p.Notes,
            Installed = p.Installed,
            InstallPath = p.InstallPath,
            FpsCap = p.Id.Contains("valorant", StringComparison.OrdinalIgnoreCase) ? 240u : 0u,
            Priority = p.Id.Contains("cs2", StringComparison.OrdinalIgnoreCase) ? "High" : "Normal"
        }).ToList();

    public CommandResult RunCleaner(CleanOptions options)
    {
        var result = _cleanerService.Run(options);
        if (result.Items.Count == 0)
        {
            return CommandResult.Ok(_l10n.CleanerNoOptionsSelected());
        }

        return CommandResult.Ok(_l10n.CleanerSuccess(result.FreedMb, result.Items.Count));
    }

    public PingResult RunPing(string host)
    {
        try
        {
            using var ping = new Ping();
            var reply = ping.Send(host, 2000);
            return new PingResult
            {
                Host = host,
                LatencyMs = reply.Status == IPStatus.Success ? reply.RoundtripTime : null,
                PacketLoss = reply.Status == IPStatus.Success ? 0 : 100,
                Message = reply.Status.ToString()
            };
        }
        catch (Exception ex)
        {
            return new PingResult
            {
                Host = host,
                Message = ex.Message
            };
        }
    }

    public CommandResult ApplyGodModePowerPlan() => Localize(_powerPlanService.ApplyGodModePlan());

    public UnigineBenchmarkStatus GetUnigineBenchmarkStatus() => _unigineBenchmarkService.GetStatus();

    public UnigineBenchmarkStatus GetGpuBenchmarkStatus() =>
        _spaceBattleBenchmarkService.GetStatus();

    public Task<CommandResult> EnsureGpuBenchmarkAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default) =>
        EnsureGpuBenchmarkInternalAsync(progress, cancellationToken);

    private async Task<CommandResult> EnsureGpuBenchmarkInternalAsync(
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        if (_spaceBattleBenchmarkService.GetStatus().Available)
        {
            await _presentMonService.EnsureInstalledAsync(progress, cancellationToken).ConfigureAwait(false);
            return CommandResult.Ok(_spaceBattleBenchmarkService.GetStatus().Message);
        }

        var (spaceOk, spaceMessage) = await _spaceBattleBenchmarkService
            .EnsureInstalledAsync(progress, cancellationToken)
            .ConfigureAwait(false);
        if (spaceOk)
        {
            await _presentMonService.EnsureInstalledAsync(progress, cancellationToken).ConfigureAwait(false);
            return CommandResult.Ok(spaceMessage);
        }

        return await EnsureUnigineBenchmarkInternalAsync(progress, cancellationToken).ConfigureAwait(false);
    }

    public Task<CommandResult> EnsureUnigineBenchmarkAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default) =>
        EnsureUnigineBenchmarkInternalAsync(progress, cancellationToken);

    private async Task<CommandResult> EnsureUnigineBenchmarkInternalAsync(
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var (success, message) = await _unigineBenchmarkService.EnsureInstalledAsync(progress, cancellationToken)
            .ConfigureAwait(false);
        if (!success)
        {
            return CommandResult.Err(message);
        }

        await _presentMonService.EnsureInstalledAsync(progress, cancellationToken).ConfigureAwait(false);
        return CommandResult.Ok(message);
    }

    public Task<CommandResult> EnsurePresentMonAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default) =>
        EnsurePresentMonInternalAsync(progress, cancellationToken);

    private async Task<CommandResult> EnsurePresentMonInternalAsync(
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var (success, message) = await _presentMonService.EnsureInstalledAsync(progress, cancellationToken)
            .ConfigureAwait(false);
        return success ? CommandResult.Ok(message) : CommandResult.Err(message);
    }

    public (bool Success, string Message, UnigineBenchmarkSession? Session) StartUnigineBenchmark(uint durationSecs) =>
        _unigineBenchmarkService.StartBenchmark(durationSecs);

    public async Task<CommandResult> RunAutomatedHeavenBenchmarkAsync(
        string label,
        uint durationSecs,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedLabel = string.IsNullOrWhiteSpace(label) ? _l10n.BenchmarkDefaultLabel : label;
        var useSpaceBattle = _spaceBattleBenchmarkService.GetStatus().Available;
        var result = useSpaceBattle
            ? await _spaceBattleBenchmarkService
                .RunAutomatedBenchmarkAsync(durationSecs, progress, cancellationToken)
                .ConfigureAwait(false)
            : await _unigineBenchmarkService
                .RunAutomatedBenchmarkAsync(durationSecs, progress, cancellationToken)
                .ConfigureAwait(false);

        if (!result.Success)
        {
            return CommandResult.Err(result.Message);
        }

        var telemetry = _telemetryService.Collect();
        var record = new BenchmarkSession
        {
            Label = resolvedLabel,
            TakenAt = DateTimeOffset.Now.ToString("s"),
            DurationSecs = result.DurationSecs,
            AvgFps = result.AvgFps,
            Pct1Low = result.MinFps,
            AvgFrametimeMs = result.AvgFps is float fps && fps > 0f ? 1000f / fps : null,
            AvgCpuPct = telemetry.CpuUsagePct,
            AvgGpuPct = telemetry.GpuUsagePct,
            Source = string.IsNullOrWhiteSpace(result.ScoreSource) ? result.Engine : result.ScoreSource,
            Notes = result.MinFps is float min && result.MaxFps is float max
                ? $"3D benchmark — min {min:F1} / max {max:F1} FPS"
                : result.AvgFps is float avg
                    ? $"3D benchmark — {avg:F1} FPS average"
                    : "3D fly-through benchmark",
        };
        _guardianDatabase.SaveBenchmark(record);
        return CommandResult.Ok(result.Message);
    }

    public CommandResult CompleteUnigineBenchmark(string label, UnigineBenchmarkSession session)
    {
        var resolvedLabel = string.IsNullOrWhiteSpace(label) ? _l10n.BenchmarkDefaultLabel : label;
        var result = _unigineBenchmarkService.CompleteBenchmark(session);
        if (!result.Success)
        {
            return CommandResult.Err(result.Message);
        }

        var telemetry = _telemetryService.Collect();
        var record = new BenchmarkSession
        {
            Label = resolvedLabel,
            TakenAt = DateTimeOffset.Now.ToString("s"),
            DurationSecs = result.DurationSecs > 0 ? result.DurationSecs : session.DurationSecs,
            AvgFps = result.AvgFps,
            Pct1Low = result.MinFps,
            AvgFrametimeMs = result.AvgFps is float fps && fps > 0f ? 1000f / fps : null,
            AvgCpuPct = telemetry.CpuUsagePct,
            AvgGpuPct = telemetry.GpuUsagePct,
            Source = string.IsNullOrWhiteSpace(result.ScoreSource) ? result.Engine : result.ScoreSource,
            Notes = result.MinFps is float min && result.MaxFps is float max
                ? $"3D benchmark — min {min:F1} / max {max:F1} FPS"
                : result.AvgFps is float avg
                    ? $"3D benchmark — {avg:F1} FPS average"
                    : "3D fly-through benchmark",
        };
        _guardianDatabase.SaveBenchmark(record);
        return CommandResult.Ok(result.Message);
    }

    public AppSettings GetAppSettings() => _stateStore.GetSettings();

    public CommandResult SaveAppSettings(AppSettings settings)
    {
        _stateStore.SetSettings(settings);
        var safety = _guardianDatabase.GetSettings();
        safety.Language = settings.Language;
        _guardianDatabase.SaveSettings(safety);
        _l10n.SetLanguage(settings.Language);
        return CommandResult.Ok(_l10n.T("Settings saved.", "บันทึกการตั้งค่าแล้ว"));
    }

    public IReadOnlyList<TweakDefinition> GetGuardianTweaks() => _guardianEngine.Registry();

    public bool IsGuardianTweakApplied(string id) =>
        _guardianDatabase.AppliedIds().Any(x => string.Equals(x, id, StringComparison.OrdinalIgnoreCase));

    public CommandResult SetGuardianTweakState(string id, bool enabled)
    {
        var result = enabled ? _guardianEngine.Apply(id) : _guardianEngine.Revert(id);
        return Localize(result);
    }

    public SpecGateResult SpecGate(string tweakId)
    {
        var gate = _safetyService.SpecGate(tweakId);
        return new SpecGateResult
        {
            Allowed = gate.Allowed,
            Message = _l10n.SpecGateMessage(gate.Message),
            CpuTempC = gate.CpuTempC,
            GpuTempC = gate.GpuTempC,
            RequiresConfirmTimer = gate.RequiresConfirmTimer,
        };
    }

    public ExpertRiskStatus GetExpertRiskStatus() => _stateStore.GetExpertRiskStatus();

    public void WaiveExpertRisk() => _stateStore.WaiveExpertRisk();

    public void ClearExpertRiskWaiver() => _stateStore.ClearExpertRiskWaiver();

    public IReadOnlyList<ExpertGuide> GetExpertGuides() => ExpertGuideCatalog.All;

    public CommandResult ConfirmPendingRevert() => Localize(_safetyService.ConfirmPendingRevert());

    public CommandResult RejectPendingRevert() => Localize(_safetyService.RejectPendingRevert());

    public LocalizationService Localization => _l10n;

    public string? RunBootSafetyCheck()
    {
        var msg = _safetyService.RunBootChecksOnce();
        if (string.IsNullOrWhiteSpace(msg))
        {
            return null;
        }

        if (msg.StartsWith("Boot auto-revert:", StringComparison.OrdinalIgnoreCase))
        {
            var reason = msg["Boot auto-revert:".Length..].Trim();
            return _l10n.BootAutoRevert(reason);
        }

        if (msg.StartsWith("Crash watchdog:", StringComparison.OrdinalIgnoreCase))
        {
            return _l10n.BootCrashWatchdog();
        }

        return msg;
    }

    public SafetySettings GetSafetySettings() => _guardianDatabase.GetSettings();

    public CommandResult SaveSafetySettings(SafetySettings settings)
    {
        _guardianDatabase.SaveSettings(settings);
        return Localize(CommandResult.Ok("Safety settings saved."));
    }

    public string GetDataDirectory() => _stateStore.DataDirectory;

    public string GetGuardianDataDirectory() => _guardianDatabase.DataDirectory;

    private static List<WatcherProfile> BuildDefaultProfiles() =>
    [
        new WatcherProfile
        {
            Id = "cs2",
            Name = "Counter-Strike 2",
            Executable = @"C:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive\game\bin\win64\cs2.exe",
            TweakIds = [ "cpu-game-priority", "net-throttling", "win-mmcss-latency" ],
            WatcherEnabled = true,
            LaunchOptions = "-high -novid",
            Notes = [ "cs2-overlays" ]
        },
        new WatcherProfile
        {
            Id = "valorant",
            Name = "VALORANT",
            Executable = @"C:\Riot Games\VALORANT\live\VALORANT.exe",
            TweakIds = [ "cpu-core-parking", "net-nagle", "win-priority-26" ],
            WatcherEnabled = false,
            LaunchOptions = string.Empty,
            Notes = [ "valorant-latency" ]
        }
    ];
}

public static class AppServicesRegistration
{
    public static IServiceCollection AddFpsGodPcAppServices(this IServiceCollection services)
    {
        services.AddSingleton<ProcessRunner>();
        services.AddSingleton<AppStateStore>();
        services.AddSingleton<GuardianDatabase>();
        services.AddSingleton<ElevationHelper>();
        services.AddSingleton<HardwareMonitorService>();
        services.AddSingleton<TelemetryService>();
        services.AddSingleton<SystemInfoService>();
        services.AddSingleton<LocalizationService>();
        services.AddSingleton<GuardianEngine>();
        services.AddSingleton<SafetyService>();
        services.AddSingleton<TweakEngine>();
        services.AddSingleton<BoostService>();
        services.AddSingleton<RestoreService>();
        services.AddSingleton<CleanerService>();
        services.AddSingleton<GameInstallDetector>();
        services.AddSingleton<PowerPlanService>();
        services.AddSingleton<UnigineBenchmarkService>();
        services.AddSingleton<SpaceBattleBenchmarkService>();
        services.AddSingleton<PresentMonService>();
        services.AddSingleton<GameWatcherService>(sp =>
        {
            var engine = sp.GetRequiredService<TweakEngine>();
            var db = sp.GetRequiredService<GuardianDatabase>();
            return new GameWatcherService(
                db,
                id =>
                {
                    var result = engine.ApplyTweak(id);
                    return (result.Success, result.Message);
                },
                id =>
                {
                    var result = engine.RevertTweak(id);
                    return (result.Success, result.Message);
                });
        });
        services.AddSingleton<AppServices>();
        return services;
    }
}
