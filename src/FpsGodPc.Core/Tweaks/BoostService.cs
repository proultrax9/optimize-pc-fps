using FpsGodPc.Core.Models;
using FpsGodPc.Services;

namespace FpsGodPc.Core.Tweaks;

public sealed class BoostService
{
    private readonly TweakEngine _engine;
    private readonly AppStateStore _store;

    public BoostService(TweakEngine engine, AppStateStore store)
    {
        _engine = engine;
        _store = store;
    }

    public static IReadOnlyList<string> GetPresetIds(string preset) => preset switch
    {
        "safe" => ["win-game-mode", "win-power-high", "win-visual-fx", "win-bg-apps", "gpu-shader-cache", "cpu-game-priority", "net-dns-flush"],
        "competitive" => ["win-game-mode", "win-power-high", "win-visual-fx", "win-game-dvr", "win-telemetry", "win-fullscreen-opt", "win-bg-apps", "gpu-shader-cache", "gpu-max-perf", "gpu-low-latency", "cpu-game-priority", "cpu-timer-res", "net-dns-flush", "net-adapter-power", "net-nagle", "win-disable-power-saving", "win-priority-26", "win-mmcss-latency"],
        "extreme" => ["win-game-mode", "win-power-high", "win-visual-fx", "win-game-dvr", "win-telemetry", "win-fullscreen-opt", "win-bg-apps", "gpu-shader-cache", "gpu-max-perf", "gpu-low-latency", "cpu-game-priority", "cpu-core-parking", "cpu-timer-res", "net-dns-flush", "net-adapter-power", "net-nagle", "net-throttling", "win-priority-26", "win-mmcss-latency", "win-system-ini-fps", "win-disable-power-saving", "adv-ram-standby"],
        "expert" => ["gpu-hags-advisor", "cpu-undervolt", "adv-vbs-warn", "adv-bios-xmp"],
        _ => [],
    };

    public ApplyBoostResult ApplyBoost(string preset)
    {
        var ids = GetPresetIds(preset);
        var applied = new List<string>();
        var failed = new List<FailedTweak>();
        _store.BeginBatch();

        foreach (var id in ids)
        {
            if (TweakCatalog.IsAdvisorOnly(id))
            {
                _store.MarkApplied(id, null);
                applied.Add(id);
                continue;
            }

            var result = _engine.ApplyTweak(id);
            if (result.Success) applied.Add(id);
            else failed.Add(new FailedTweak { Id = id, Error = result.Message });
        }

        var name = preset switch { "safe" => "Safe Boost", "competitive" => "Competitive Boost", "extreme" => "Extreme Boost", "expert" => "Expert Guide", _ => preset };
        _store.SetLastBoost(name);
        _store.EndBatch();

        return new ApplyBoostResult
        {
            Applied = applied,
            Failed = failed,
            Message = failed.Count == 0
                ? $"{name} applied successfully ({applied.Count} tweaks)."
                : $"{name}: {applied.Count} applied, {failed.Count} failed. Some tweaks need Administrator.",
        };
    }
}
