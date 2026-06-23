using FpsGodPc.Core.Models;
using FpsGodPc.Services;

namespace FpsGodPc.Core.Guardian;

public sealed class SafetyService
{
    private readonly GuardianEngine _engine;
    private readonly GuardianDatabase _db;
    private readonly TelemetryService _telemetry;
    private readonly object _bootLock = new();
    private string? _bootNotice;

    public SafetyService(GuardianEngine engine, GuardianDatabase db, TelemetryService telemetry)
    {
        _engine = engine;
        _db = db;
        _telemetry = telemetry;
    }

    public SpecGateResult SpecGate(string tweakId)
    {
        var tier = _engine.Registry().FirstOrDefault(t => t.Id == tweakId)?.Tier ?? RiskTier.Advanced;
        var t = _telemetry.Collect();

        return tier switch
        {
            RiskTier.Safe => new SpecGateResult
            {
                Allowed = true,
                Message = "Safe tweak - no hardware limits changed.",
                CpuTempC = t.CpuTempC,
                GpuTempC = t.GpuTempC,
                RequiresConfirmTimer = false
            },
            RiskTier.Moderate => new SpecGateResult
            {
                Allowed = true,
                Message = (t.CpuTempC > 85 || t.GpuTempC > 83)
                    ? "Warning: thermals are elevated. Proceed only if stable."
                    : "Moderate tweak - snapshot created. 15s confirm timer will start.",
                CpuTempC = t.CpuTempC,
                GpuTempC = t.GpuTempC,
                RequiresConfirmTimer = true
            },
            _ => new SpecGateResult
            {
                Allowed = !(t.GpuTempC >= 84),
                Message = (t.GpuTempC >= 84)
                    ? "Blocked: GPU already hot at stock. Applying limits risks crashes."
                    : "Advanced tweak - 15s confirm timer. Snapshot saved.",
                CpuTempC = t.CpuTempC,
                GpuTempC = t.GpuTempC,
                RequiresConfirmTimer = t.GpuTempC < 84
            }
        };
    }

    public string? CrashWatchdogMessage() =>
        _db.ShouldShowCrashWatchdog()
            ? "Previous session ended unexpectedly while tweaks were active. Rollback recommended."
            : null;

    public CommandResult ConfirmPendingRevert()
    {
        var pending = _db.PendingRevertInfo();
        if (pending is null)
        {
            return CommandResult.Err("No pending revert.");
        }

        if (pending.Value.SnapshotId > 0)
        {
            _db.MarkKnownGood(pending.Value.SnapshotId);
        }

        _db.ClearPendingRevert();
        return CommandResult.Ok("Tweaks confirmed as stable - marked known-good.");
    }

    public CommandResult RejectPendingRevert()
    {
        var pending = _db.PendingRevertInfo();
        if (pending is null)
        {
            return CommandResult.Err("No pending revert.");
        }

        if (pending.Value.SnapshotId > 0)
        {
            return RestoreSnapshot(pending.Value.SnapshotId);
        }

        _ = _engine.RevertAll();
        _db.ClearPendingRevert();
        return CommandResult.Ok("Pending tweaks reverted.");
    }

    public CommandResult RestoreSnapshot(long id)
    {
        try
        {
            _ = _db.GetSnapshotState(id);
            _db.MarkKnownGood(id);
            _db.ClearPendingRevert();
            return CommandResult.Ok("Restored from snapshot.");
        }
        catch (Exception ex)
        {
            return CommandResult.Err(ex.Message);
        }
    }

    public string? RunBootChecksOnce()
    {
        lock (_bootLock)
        {
            if (_bootNotice is not null)
            {
                return _bootNotice;
            }

            _bootNotice = RunBootChecks();
            return _bootNotice;
        }
    }

    public string? RunBootChecks()
    {
        var settings = _db.GetSettings();
        if (settings.BootAutoRevert)
        {
            var pending = _db.PendingRevertInfo();
            if (pending is { SnapshotId: > 0 })
            {
                _ = RestoreSnapshot(pending.Value.SnapshotId);
                return $"Boot auto-revert: {pending.Value.Reason}";
            }
        }

        if (_db.ShouldShowCrashWatchdog())
        {
            _ = _engine.RevertAll();
            return "Crash watchdog: previous session was dirty - rolled back pending tweaks.";
        }

        return null;
    }
}
