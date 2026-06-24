using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;

namespace FpsGodPc.Services;

/// <summary>
/// GPU benchmark: Hypertune Space Battle when installed, otherwise GitHub ComTechSpaceShooter, else Heaven fallback.
/// </summary>
public sealed class SpaceBattleBenchmarkService
{
    private const string HypertuneExeName = "HyperTuneBenchmark.exe";
    private const string HypertuneBuildFolder = "HyperTuneBenchmark_Build_Spacebattle";

    private const string GitHubDownloadUrl =
        "https://github.com/AntonHedlund/ComTechSpaceShooter/releases/download/BurstCompile-Everything/BurstVersion100KAsteroids.zip";

    private const string InstallFolderName = "SpaceBattle";
    private const string ZipFileName = "BurstVersion100KAsteroids.zip";
    private const long ZipMinBytes = 25_000_000;

    private static readonly string[] ExecutableNames =
    [
        HypertuneExeName,
        "ComTechSpaceShooter.exe",
        "SpaceBattleArcade.exe",
        "Space_Battle_Arcade.exe",
        "SpaceArcade.exe",
    ];

    private readonly PresentMonService _presentMon;
    private string? _cachedExecutable;
    private bool _cachedIsHypertune;

    public SpaceBattleBenchmarkService(PresentMonService presentMon)
    {
        _presentMon = presentMon;
    }

    public void InvalidateCache()
    {
        _cachedExecutable = null;
        _cachedIsHypertune = false;
    }

    public bool IsHypertuneBenchmark() =>
        ResolveExecutable() is not null && _cachedIsHypertune;

    public UnigineBenchmarkStatus GetStatus()
    {
        var folder = GetInstallFolder();
        var exe = ResolveExecutable();
        if (exe is null)
        {
            return new UnigineBenchmarkStatus
            {
                Available = false,
                InstallFolder = folder,
                Engine = "Space Battle",
                Message = "No space benchmark found — install Hypertune or click Launch to download from GitHub (~32 MB).",
            };
        }

        if (_cachedIsHypertune)
        {
            return new UnigineBenchmarkStatus
            {
                Available = true,
                ExecutablePath = exe,
                Engine = "Space Battle",
                InstallFolder = Path.GetDirectoryName(exe)!,
                Message = "Hypertune Space Battle ready — same 3D scene as Hypertune. Auto-runs for 60s.",
            };
        }

        return new UnigineBenchmarkStatus
        {
            Available = true,
            ExecutablePath = exe,
            Engine = "Space Battle",
            InstallFolder = folder,
            Message = "Space Battle ready — 100K asteroid stress test from GitHub. Auto-runs for 60s.",
        };
    }

    public async Task<(bool Success, string Message)> EnsureInstalledAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (ResolveHypertuneExecutable() is not null)
        {
            return (true, GetStatus().Message);
        }

        if (ResolveBundledExecutable() is not null)
        {
            return (true, GetStatus().Message);
        }

        var folder = GetInstallFolder();
        Directory.CreateDirectory(folder);
        var zipPath = Path.Combine(folder, ZipFileName);

        try
        {
            if (!IsValidZip(zipPath))
            {
                progress?.Report("Downloading Space Battle from GitHub (~32 MB)...");
                await DownloadBuildAsync(zipPath, progress, cancellationToken).ConfigureAwait(false);
            }

            progress?.Report("Extracting Space Battle benchmark...");
            ExtractZip(zipPath, folder);
            InvalidateCache();

            for (var i = 0; i < 20; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (ResolveBundledExecutable() is not null)
                {
                    InvalidateCache();
                    return (true, "Space Battle benchmark installed — ready to launch.");
                }

                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }

            return (false, "Extract finished but the game executable was not found. Delete the SpaceBattle folder and try again.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return (false, $"Failed to install Space Battle: {ex.Message}");
        }
    }

    public async Task<UnigineBenchmarkRunResult> RunAutomatedBenchmarkAsync(
        uint durationSeconds,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        durationSeconds = Math.Clamp(durationSeconds, 10u, 600u);
        await _presentMon.EnsureInstalledAsync(progress, cancellationToken).ConfigureAwait(false);

        var exe = ResolveExecutable();
        if (exe is null)
        {
            return new UnigineBenchmarkRunResult
            {
                Success = false,
                Engine = "Space Battle",
                DurationSecs = durationSeconds,
                Message = "Space Battle benchmark is not installed.",
            };
        }

        var isHypertune = _cachedIsHypertune;
        var workDir = Path.GetDirectoryName(exe)!;
        var presentMonProcessName = isHypertune ? "HyperTuneBenchmark" : Path.GetFileNameWithoutExtension(exe);
        var startedAt = DateTime.UtcNow;
        var presentMonCsv = Path.Combine(
            UnigineBenchmarkService.GetReportsFolder(),
            $"spacebattle-{startedAt:yyyyMMdd_HHmmss}.csv");

        Process? process = null;
        Process? presentMonProcess = null;
        try
        {
            progress?.Report(isHypertune
                ? "Launching Hypertune Space Battle..."
                : "Launching Space Battle benchmark...");
            process = Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                WorkingDirectory = workDir,
                UseShellExecute = true,
            });

            if (process is null)
            {
                return new UnigineBenchmarkRunResult
                {
                    Success = false,
                    Engine = "Space Battle",
                    DurationSecs = durationSeconds,
                    Message = "Failed to start the space benchmark.",
                };
            }

            progress?.Report("Loading space battle scene...");
            if (!await HeavenAutomation.WaitForMainWindowAsync(process, TimeSpan.FromSeconds(90), cancellationToken)
                    .ConfigureAwait(false))
            {
                return new UnigineBenchmarkRunResult
                {
                    Success = false,
                    Engine = "Space Battle",
                    DurationSecs = durationSeconds,
                    Message = "Space benchmark window did not open.",
                };
            }

            await Task.Delay(12_000, cancellationToken).ConfigureAwait(false);
            HeavenAutomation.FocusWindow(process);

            progress?.Report($"Running {durationSeconds}s space battle benchmark...");
            presentMonProcess = _presentMon.StartTimedCapture(
                durationSeconds + 15,
                presentMonCsv,
                presentMonProcessName);

            await Task.Delay(TimeSpan.FromSeconds(durationSeconds), cancellationToken).ConfigureAwait(false);

            progress?.Report("Saving benchmark results...");
            HeavenAutomation.KillProcessTree(process);
            process = null;
            WaitForPresentMon(presentMonProcess);
            presentMonProcess = null;
            await Task.Delay(2000, cancellationToken).ConfigureAwait(false);

            var scores = ParseResults(workDir, startedAt, presentMonCsv, isHypertune);
            if (scores.AvgFps is null)
            {
                return new UnigineBenchmarkRunResult
                {
                    Success = true,
                    Engine = "Space Battle",
                    DurationSecs = durationSeconds,
                    ScoreSource = "Space Battle",
                    Message = "Benchmark finished but FPS was not captured. Run the app as Administrator and try again.",
                };
            }

            return new UnigineBenchmarkRunResult
            {
                Success = true,
                Engine = "Space Battle",
                DurationSecs = durationSeconds,
                AvgFps = scores.AvgFps,
                MinFps = scores.MinFps,
                MaxFps = scores.MaxFps,
                ScoreSource = scores.Source,
                Message = $"Done — {scores.AvgFps:F1} FPS average over {durationSeconds}s ({scores.Source}).",
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new UnigineBenchmarkRunResult
            {
                Success = false,
                Engine = "Space Battle",
                DurationSecs = durationSeconds,
                Message = $"Space benchmark failed: {ex.Message}",
            };
        }
        finally
        {
            HeavenAutomation.KillProcessTree(process);
            TryStopPresentMon(presentMonProcess);
        }
    }

    public static string GetInstallFolder() =>
        Path.Combine(UnigineBenchmarkService.GetBenchmarkFolder(), InstallFolderName);

    private string? ResolveExecutable(bool forceRefresh = false)
    {
        if (!forceRefresh && _cachedExecutable is not null && File.Exists(_cachedExecutable))
        {
            return _cachedExecutable;
        }

        if (ResolveHypertuneExecutable() is { } hypertune)
        {
            _cachedExecutable = hypertune;
            _cachedIsHypertune = true;
            return hypertune;
        }

        if (ResolveBundledExecutable() is { } bundled)
        {
            _cachedExecutable = bundled;
            _cachedIsHypertune = false;
            return bundled;
        }

        _cachedExecutable = null;
        _cachedIsHypertune = false;
        return null;
    }

    private static string? ResolveHypertuneExecutable()
    {
        foreach (var root in GetHypertuneSearchRoots())
        {
            var path = Path.Combine(root, HypertuneExeName);
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private static string? ResolveBundledExecutable() => FindExecutable(GetInstallFolder());

    private static IEnumerable<string> GetHypertuneSearchRoots()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        yield return Path.Combine(localAppData, "Programs", "CompReady", "HyperTune", HypertuneBuildFolder);
        yield return Path.Combine(programFiles, "CompReady", "HyperTune", HypertuneBuildFolder);
        yield return Path.Combine(programFilesX86, "CompReady", "HyperTune", HypertuneBuildFolder);
    }

    private static string? FindExecutable(string root)
    {
        if (!Directory.Exists(root))
        {
            return null;
        }

        foreach (var name in ExecutableNames)
        {
            if (name.Equals(HypertuneExeName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var match = Directory.EnumerateFiles(root, name, SearchOption.AllDirectories)
                    .FirstOrDefault(File.Exists);
                if (match is not null)
                {
                    return match;
                }
            }
            catch
            {
                // Best effort.
            }
        }

        return null;
    }

    private BenchmarkScores ParseResults(string workDir, DateTime startedAtUtc, string presentMonCsv, bool isHypertune)
    {
        if (isHypertune)
        {
            var frameLog = FindNewestFrameLog(workDir, startedAtUtc);
            if (frameLog is not null)
            {
                var fromLog = ParseFrameLogsCsv(frameLog);
                if (fromLog.AvgFps is not null)
                {
                    return new BenchmarkScores(fromLog.AvgFps, fromLog.MinFps, fromLog.MaxFps, "space-battle");
                }
            }
        }

        if (File.Exists(presentMonCsv))
        {
            var pm = _presentMon.TryParseCsvFile(presentMonCsv);
            if (pm?.AvgFps is float avg)
            {
                return new BenchmarkScores(avg, pm.MinFps, avg, "presentmon");
            }
        }

        return new BenchmarkScores(null, null, null, "Space Battle");
    }

    private static string? FindNewestFrameLog(string workDir, DateTime startedAtUtc)
    {
        var logsDir = Path.Combine(workDir, "Logs");
        if (!Directory.Exists(logsDir))
        {
            return null;
        }

        try
        {
            return Directory.EnumerateFiles(logsDir, "frame_logs.csv", SearchOption.AllDirectories)
                .Select(f => new FileInfo(f))
                .Where(f => f.LastWriteTimeUtc >= startedAtUtc.AddMinutes(-1))
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .FirstOrDefault()
                ?.FullName;
        }
        catch
        {
            return null;
        }
    }

    private static (float? AvgFps, float? MinFps, float? MaxFps) ParseFrameLogsCsv(string csvPath)
    {
        try
        {
            var fpsValues = new List<float>();
            var minValues = new List<float>();
            var maxValues = new List<float>();

            foreach (var line in File.ReadLines(csvPath))
            {
                if (string.IsNullOrWhiteSpace(line) ||
                    line.StartsWith("sep=", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("time;", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var cols = line.Split(';');
                if (cols.Length < 4)
                {
                    continue;
                }

                if (!float.TryParse(cols[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var fps) ||
                    fps < 20f || fps > 1000f)
                {
                    continue;
                }

                fpsValues.Add(fps);
                if (float.TryParse(cols[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var min))
                {
                    minValues.Add(min);
                }

                if (float.TryParse(cols[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var max))
                {
                    maxValues.Add(max);
                }
            }

            if (fpsValues.Count < 10)
            {
                return (null, null, null);
            }

            return (
                fpsValues.Average(),
                minValues.Count > 0 ? minValues.Min() : null,
                maxValues.Count > 0 ? maxValues.Max() : null);
        }
        catch
        {
            return (null, null, null);
        }
    }

    private static bool IsValidZip(string path) =>
        File.Exists(path) && new FileInfo(path).Length >= ZipMinBytes;

    private static async Task DownloadBuildAsync(
        string destination,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(20) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("FPS-Optimize-GOD-PC/1.0");

        using var response = await client
            .GetAsync(GitHubDownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? 0;
        var tempPath = destination + ".download";
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var file = File.Create(tempPath);
        var buffer = new byte[81920];
        long read = 0;
        int bytes;
        while ((bytes = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            await file.WriteAsync(buffer.AsMemory(0, bytes), cancellationToken).ConfigureAwait(false);
            read += bytes;
            if (total > 0)
            {
                progress?.Report($"Downloading Space Battle... {read * 100 / total}%");
            }
        }

        file.Close();
        if (new FileInfo(tempPath).Length < ZipMinBytes)
        {
            File.Delete(tempPath);
            throw new InvalidOperationException("Download was incomplete. Check your internet connection and try again.");
        }

        if (File.Exists(destination))
        {
            File.Delete(destination);
        }

        File.Move(tempPath, destination);
    }

    private static void ExtractZip(string zipPath, string destination)
    {
        var extractRoot = Path.Combine(destination, "_extract");
        if (Directory.Exists(extractRoot))
        {
            Directory.Delete(extractRoot, recursive: true);
        }

        Directory.CreateDirectory(extractRoot);
        ZipFile.ExtractToDirectory(zipPath, extractRoot, overwriteFiles: true);

        foreach (var name in ExecutableNames)
        {
            if (name.Equals(HypertuneExeName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var exe = Directory.EnumerateFiles(extractRoot, name, SearchOption.AllDirectories).FirstOrDefault();
            if (exe is null)
            {
                continue;
            }

            var targetDir = Path.GetDirectoryName(exe)!;
            CopyDirectory(targetDir, destination);
            Directory.Delete(extractRoot, recursive: true);
            return;
        }

        CopyDirectory(extractRoot, destination);
        Directory.Delete(extractRoot, recursive: true);
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    private static void WaitForPresentMon(Process? process)
    {
        if (process is null)
        {
            return;
        }

        try
        {
            if (!process.HasExited)
            {
                process.WaitForExit(120_000);
            }
        }
        catch
        {
            // Best effort.
        }
    }

    private static void TryStopPresentMon(Process? process)
    {
        if (process is null || process.HasExited)
        {
            return;
        }

        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Best effort.
        }
    }

    private sealed record BenchmarkScores(float? AvgFps, float? MinFps, float? MaxFps, string Source);
}
