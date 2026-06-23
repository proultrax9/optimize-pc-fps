using System.Diagnostics;

namespace FpsGodPc.Services;

public sealed class WatcherStatus
{
    public bool Enabled { get; init; }
    public string? ActiveProfileId { get; init; }
    public string? ActiveExecutable { get; init; }
    public string? LastEvent { get; init; }
}

public sealed class GameWatcherService : IDisposable
{
    private readonly GuardianDatabase _database;
    private readonly Func<string, (bool Success, string Message)> _applyTweak;
    private readonly Func<string, (bool Success, string Message)> _revertTweak;

    private CancellationTokenSource? _cts;
    private string? _activeProfileId;
    private string? _lastEvent;
    private readonly object _stateLock = new();

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
        _ = Task.Run(() => PollLoop(_cts.Token));
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

    public void Dispose() => Stop();

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
                    if (!IsProcessRunning(profile.Executable))
                    {
                        continue;
                    }

                    found = profile.Id;
                    if (!string.Equals(wasRunning, profile.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        var applied = 0;
                        foreach (var tweakId in profile.TweakIds)
                        {
                            if (_applyTweak(tweakId).Success)
                            {
                                applied++;
                            }
                        }

                        SetState(profile.Id, $"Applied {applied} tweaks for {profile.Name}");
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
                            _revertTweak(tweakId);
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
