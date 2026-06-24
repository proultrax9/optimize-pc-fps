using System.Diagnostics;

namespace FpsGodPc.Services;

public sealed class WatcherStatus
{
    public bool Enabled { get; init; }
    public string? ActiveProfileId { get; init; }
    public string? ActiveExecutable { get; init; }
    public string? LastEvent { get; init; }
}

/// <summary>
/// Polls for watched game processes at 2-second intervals. When a watched profile
/// is detected, applies its tweaks via the injected delegates (identical to original
/// constructor signature — DI wiring in AppServices.cs is unchanged).
///
/// Internally uses <see cref="ProcessPriorityService"/> and
/// <see cref="TimerResolutionService"/> (instantiated as plain `new`) to handle the
/// cpu-game-priority and cpu-timer-res tweaks at the process level rather than
/// delegating to TweakEngine (which can't hold a live Process handle).
/// </summary>
public sealed class GameWatcherService : IDisposable
{
    // Tweaks that are handled natively inside the watcher rather than via _applyTweak/_revertTweak
    private const string TweakCpuGamePriority = "cpu-game-priority";
    private const string TweakCpuTimerRes = "cpu-timer-res";

    private readonly GuardianDatabase _database;
    private readonly Func<string, (bool Success, string Message)> _applyTweak;
    private readonly Func<string, (bool Success, string Message)> _revertTweak;

    // Instantiated as plain `new` — no DI needed for these lightweight services.
    private readonly ProcessPriorityService _priority = new();
    private readonly TimerResolutionService _timer = new();

    private CancellationTokenSource? _cts;
    private Task? _pollTask;
    private string? _activeProfileId;
    private string? _lastEvent;
    private readonly object _stateLock = new();

    /// <summary>
    /// Constructor signature is IDENTICAL to the original — DI wiring in AppServices.cs
    /// does not need to change.
    /// </summary>
    public GameWatcherService(
        GuardianDatabase database,
        Func<string, (bool Success, string Message)> applyTweak,
        Func<string, (bool Success, string Message)> revertTweak)
    {
        _database = database;
        _applyTweak = applyTweak;
        _revertTweak = revertTweak;
    }

    public void Start()
    {
        if (_cts is not null)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _pollTask = Task.Run(() => PollLoop(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    public WatcherStatus GetStatus()
    {
        lock (_stateLock)
        {
            var settings = _database.GetSettings();
            var activeProfile = _activeProfileId is null
                ? null
                : _database.ListProfiles().FirstOrDefault(p => p.Id == _activeProfileId);

            return new WatcherStatus
            {
                Enabled = settings.WatcherEnabled && _cts is not null,
                ActiveProfileId = _activeProfileId,
                ActiveExecutable = activeProfile?.Executable,
                LastEvent = _lastEvent,
            };
        }
    }

    public void Dispose()
    {
        Stop();
        // Wait for the poll loop to observe cancellation before releasing the timer
        // resolution handle, so the loop cannot re-Enable() it after Dispose().
        try { _pollTask?.Wait(2500); } catch { /* cancelled/faulted — expected */ }
        _timer.Dispose();
    }

    private async Task PollLoop(CancellationToken token)
    {
        string? wasRunning = null;

        while (!token.IsCancellationRequested)
        {
            try
            {
                var settings = _database.GetSettings();
                if (!settings.WatcherEnabled)
                {
                    await Task.Delay(2000, token);
                    continue;
                }

                var candidates = _database.ListProfiles().Where(p => p.WatcherEnabled).ToList();
                string? found = null;

                foreach (var profile in candidates)
                {
                    var matchedProcess = FindProcess(profile.Executable);
                    if (matchedProcess is null)
                    {
                        continue;
                    }

                    found = profile.Id;
                    if (!string.Equals(wasRunning, profile.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        var applied = 0;
                        foreach (var tweakId in profile.TweakIds)
                        {
                            if (tweakId == TweakCpuGamePriority)
                            {
                                // Apply priority directly on the detected process object
                                var (success, _) = _priority.SetPriority(matchedProcess, ProcessPriorityClass.High);
                                if (success) applied++;
                            }
                            else if (tweakId == TweakCpuTimerRes)
                            {
                                // Enable elevated timer resolution while game runs
                                _timer.Enable();
                                applied++;
                            }
                            else if (_applyTweak(tweakId).Success)
                            {
                                applied++;
                            }
                        }

                        matchedProcess.Dispose();
                        SetState(profile.Id, $"Applied {applied} tweaks for {profile.Name}");
                    }
                    else
                    {
                        matchedProcess.Dispose();
                    }

                    break;
                }

                if (found is null && wasRunning is not null)
                {
                    var profile = _database.ListProfiles().FirstOrDefault(p => p.Id == wasRunning);
                    if (profile is not null)
                    {
                        foreach (var tweakId in profile.TweakIds)
                        {
                            if (tweakId == TweakCpuTimerRes)
                            {
                                // Release elevated timer resolution when game exits
                                _timer.Disable();
                            }
                            else if (tweakId != TweakCpuGamePriority)
                            {
                                // cpu-game-priority: process is gone, nothing to restore
                                _revertTweak(tweakId);
                            }
                        }

                        SetState(null, $"Reverted tweaks for {profile.Name}");
                    }
                    else
                    {
                        SetState(null, null);
                    }
                }

                wasRunning = found;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Keep watcher alive after transient errors.
            }

            try
            {
                await Task.Delay(2000, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void SetState(string? activeProfileId, string? lastEvent)
    {
        lock (_stateLock)
        {
            _activeProfileId = activeProfileId;
            if (lastEvent is not null)
            {
                _lastEvent = lastEvent;
            }
        }
    }

    /// <summary>
    /// Finds a running process whose main module path matches <paramref name="executablePath"/>.
    /// Returns the first match as a live <see cref="Process"/> (caller must Dispose), or null.
    /// This replaces the original static <c>IsProcessRunning</c> so the watcher can obtain a
    /// real Process handle for priority manipulation.
    /// </summary>
    /// <remarks>
    /// The original public static <c>IsProcessRunning</c> is preserved for external callers
    /// that may depend on it.
    /// </remarks>
    private static Process? FindProcess(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return null;
        }

        var processName = Path.GetFileNameWithoutExtension(executablePath);
        Process? match = null;
        foreach (var proc in Process.GetProcessesByName(processName))
        {
            // Once a match is chosen, dispose every remaining enumerated handle so we
            // never leak the unexamined tail of the GetProcessesByName array (this runs
            // every 2s, so a leak here accumulates handles fast).
            if (match is not null)
            {
                proc.Dispose();
                continue;
            }

            try
            {
                // Try to match the full path for accuracy; fall back to name-match if access denied.
                if (proc.MainModule?.FileName is { } path &&
                    string.Equals(path, executablePath, StringComparison.OrdinalIgnoreCase))
                {
                    match = proc; // caller owns Dispose
                }
                else if (proc.MainModule is null)
                {
                    // Access-denied to MainModule — accept on name match alone (same as original)
                    match = proc;
                }
                else
                {
                    proc.Dispose();
                }
            }
            catch
            {
                // Access denied reading MainModule — accept on process name match (same as original)
                match = proc;
            }
        }

        return match;
    }

    /// <summary>
    /// Preserved for any external callers. Delegates to <see cref="FindProcess"/>.
    /// </summary>
    public static bool IsProcessRunning(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return false;
        }

        var processName = Path.GetFileNameWithoutExtension(executablePath);
        foreach (var process in Process.GetProcessesByName(processName))
        {
            using (process)
            {
                try
                {
                    if (process.MainModule?.FileName is { } path &&
                        string.Equals(path, executablePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                catch
                {
                    return true;
                }
            }
        }

        return false;
    }
}
