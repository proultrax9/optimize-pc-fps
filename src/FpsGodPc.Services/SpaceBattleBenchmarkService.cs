using System.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace FpsGodPc.Services;

/// <summary>
/// GPU benchmark using Unity's official open-source "Spaceship Demo" — an AAA first-person
/// space scene (HDRP + Visual Effect Graph) downloaded from GitHub on first use. Real FPS is
/// captured with PresentMon while the heavy space scene renders. No third-party branded
/// benchmark is detected or launched, so no vendor logo ever appears.
/// Source: https://github.com/Unity-Technologies/SpaceshipDemo (Unity Companion License).
/// </summary>
public sealed class SpaceBattleBenchmarkService
{
    private const string DownloadUrl =
        "https://github.com/Unity-Technologies/SpaceshipDemo/releases/download/2020.3.0/SpaceshipDemo-Release-2020.3.0-Win64.zip";

    private const string InstallFolderName = "SpaceBattle";
    private const string ZipFileName = "SpaceshipDemo-Win64.zip";
    private const long ZipMinBytes = 400_000_000; // ~560 MB build; reject truncated downloads.

    // Marker written only after a successful Spaceship Demo install. Used to distinguish this
    // build from any older benchmark (e.g. the previous asteroid demo) left in the same folder,
    // which is also a Unity build and would otherwise be picked up and launched.
    private const string BuildMarkerFile = ".spaceshipdemo-2020.3.0";

    private readonly PresentMonService _presentMon;
    private string? _cachedExecutable;

    public SpaceBattleBenchmarkService(PresentMonService presentMon)
    {
        _presentMon = presentMon;
    }

    public void InvalidateCache()
    {
        _cachedExecutable = null;
    }

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
                Message = "No space benchmark found — click Launch to download Unity's Spaceship Demo space scene (~560 MB, one time).",
            };
        }

        return new UnigineBenchmarkStatus
        {
            Available = true,
            ExecutablePath = exe,
            Engine = "Space Battle",
            InstallFolder = folder,
            Message = "Space Battle ready — AAA Unity space scene. Auto-runs for 60s and measures real FPS.",
        };
    }

    public async Task<(bool Success, string Message)> EnsureInstalledAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (IsExpectedBuildInstalled())
        {
            return (true, GetStatus().Message);
        }

        var folder = GetInstallFolder();
        // Remove any previous benchmark (e.g. the old asteroid demo) so we install the
        // Spaceship Demo cleanly rather than re-using a stale Unity build.
        TryWipeFolder(folder);
        Directory.CreateDirectory(folder);
        var zipPath = Path.Combine(folder, ZipFileName);

        try
        {
            if (!IsValidZip(zipPath))
            {
                progress?.Report("Downloading Unity Spaceship Demo (~560 MB)...");
                await DownloadBuildAsync(zipPath, progress, cancellationToken).ConfigureAwait(false);
            }

            progress?.Report("Extracting Spaceship Demo...");
            ExtractZip(zipPath, folder);
            InvalidateCache();

            for (var i = 0; i < 30; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (FindUnityExecutable(folder) is not null)
                {
                    // Stamp the marker so this build is recognised as the Spaceship Demo.
                    try { File.WriteAllText(Path.Combine(folder, BuildMarkerFile), DownloadUrl); } catch { }
                    InvalidateCache();
                    return (true, "Spaceship Demo installed — ready to launch.");
                }

                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }

            return (false, "Extract finished but the demo executable was not found. Delete the SpaceBattle folder and try again.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return (false, $"Failed to install Spaceship Demo: {ex.Message}");
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

        var workDir = Path.GetDirectoryName(exe)!;
        var presentMonProcessName = Path.GetFileNameWithoutExtension(exe);
        var startedAt = DateTime.UtcNow;
        var presentMonCsv = Path.Combine(
            UnigineBenchmarkService.GetReportsFolder(),
            $"spacebattle-{startedAt:yyyyMMdd_HHmmss}.csv");

        Process? process = null;
        Process? presentMonProcess = null;
        try
        {
            progress?.Report("Launching Space Battle (Unity Spaceship Demo)...");
            process = Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                WorkingDirectory = workDir,
                UseShellExecute = true,
                // Native fullscreen so the menu fills the primary screen and our computed
                // button coordinates map 1:1 to screen pixels.
                Arguments = "-screen-fullscreen 1",
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

            progress?.Report("Loading space scene...");
            if (!await HeavenAutomation.WaitForMainWindowAsync(process, TimeSpan.FromSeconds(120), cancellationToken)
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

            // Drive the demo's menus automatically so the user never has to click:
            //   Boot splash (auto-focuses "Play Demo") -> press Enter -> MainMenu -> click "Benchmark".
            progress?.Report("Skipping menu and starting benchmark...");
            await Task.Delay(9_000, cancellationToken).ConfigureAwait(false);
            HeavenAutomation.FocusWindow(process);
            await Task.Delay(800, cancellationToken).ConfigureAwait(false);
            HeavenAutomation.SendKey(0x0D); // Enter — confirm Boot splash if it waits for input
            await Task.Delay(6_000, cancellationToken).ConfigureAwait(false);

            // MainMenu: click the "Benchmark" button. Its RectTransform is anchored to the
            // bottom-right corner at (-311.3, 304.6) on a 1920x1080-reference canvas that scales
            // with screen height, so we map it to the current screen.
            var (sw, sh) = HeavenAutomation.GetPrimaryScreenSize();
            if (sw > 0 && sh > 0)
            {
                var scale = sh / 1080f;
                var benchX = (int)(sw - 311.3f * scale);
                var benchY = (int)(sh - 304.6f * scale);
                HeavenAutomation.FocusWindow(process);
                HeavenAutomation.ClickAt(benchX, benchY);
                await Task.Delay(600, cancellationToken).ConfigureAwait(false);
                HeavenAutomation.ClickAt(benchX, benchY);
            }

            // Let the benchmark walkthrough scene load before measuring.
            await Task.Delay(10_000, cancellationToken).ConfigureAwait(false);

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

            var scores = ParseResults(startedAt, presentMonCsv);
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
        // Only treat the install as usable when it is the Spaceship Demo (marker present),
        // so an older asteroid-demo build left in the folder is never launched.
        if (!IsExpectedBuildInstalled())
        {
            _cachedExecutable = null;
            return null;
        }

        if (!forceRefresh && _cachedExecutable is not null && File.Exists(_cachedExecutable))
        {
            return _cachedExecutable;
        }

        _cachedExecutable = FindUnityExecutable(GetInstallFolder());
        return _cachedExecutable;
    }

    private static bool IsExpectedBuildInstalled()
    {
        var folder = GetInstallFolder();
        return File.Exists(Path.Combine(folder, BuildMarkerFile)) && FindUnityExecutable(folder) is not null;
    }

    private static void TryWipeFolder(string folder)
    {
        try
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
        catch
        {
            // Best effort — a locked file just means the old build lingers alongside the new one.
        }
    }

    /// <summary>
    /// Finds the Unity standalone player exe without hard-coding its name: the player exe is
    /// the one that has a sibling "&lt;name&gt;_Data" folder. Skips Unity's crash handler.
    /// </summary>
    private static string? FindUnityExecutable(string root)
    {
        if (!Directory.Exists(root))
        {
            return null;
        }

        try
        {
            foreach (var exe in Directory.EnumerateFiles(root, "*.exe", SearchOption.AllDirectories))
            {
                var name = Path.GetFileNameWithoutExtension(exe);
                if (name.Contains("UnityCrashHandler", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var dataFolder = Path.Combine(Path.GetDirectoryName(exe)!, name + "_Data");
                if (Directory.Exists(dataFolder))
                {
                    return exe;
                }
            }
        }
        catch
        {
            // Best effort.
        }

        return null;
    }

    private BenchmarkScores ParseResults(DateTime startedAtUtc, string presentMonCsv)
    {
        // PresentMon is the reliable source of real per-frame FPS for the rendered scene.
        if (File.Exists(presentMonCsv))
        {
            var pm = _presentMon.TryParseCsvFile(presentMonCsv);
            if (pm?.AvgFps is float avg)
            {
                return new BenchmarkScores(avg, pm.MinFps, avg, "presentmon");
            }
        }

        // Secondary: the demo's own HTML benchmark report (if the user ran its built-in benchmark).
        var html = FindNewestBenchmarkReport(startedAtUtc);
        if (html is not null)
        {
            var fps = ParseHtmlAverageFps(html);
            if (fps is float h)
            {
                return new BenchmarkScores(h, null, null, "space-battle");
            }
        }

        return new BenchmarkScores(null, null, null, "Space Battle");
    }

    private static string? FindNewestBenchmarkReport(DateTime startedAtUtc)
    {
        try
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var reportDir = Path.Combine(docs, "SpaceshipDemo");
            if (!Directory.Exists(reportDir))
            {
                return null;
            }

            return Directory.EnumerateFiles(reportDir, "*.html", SearchOption.AllDirectories)
                .Select(f => new FileInfo(f))
                .Where(f => f.LastWriteTimeUtc >= startedAtUtc.AddMinutes(-2))
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .FirstOrDefault()
                ?.FullName;
        }
        catch
        {
            return null;
        }
    }

    private static float? ParseHtmlAverageFps(string htmlPath)
    {
        try
        {
            var text = File.ReadAllText(htmlPath);
            // Look for an "average ... NN.N FPS" style number in the report.
            var match = Regex.Match(text, @"(?i)average[^0-9]{0,40}([0-9]+(?:\.[0-9]+)?)\s*fps");
            if (!match.Success)
            {
                match = Regex.Match(text, @"([0-9]+(?:\.[0-9]+)?)\s*fps", RegexOptions.IgnoreCase);
            }

            if (match.Success &&
                float.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var fps) &&
                fps is > 1f and < 1000f)
            {
                return fps;
            }
        }
        catch
        {
            // Best effort.
        }

        return null;
    }

    private static bool IsValidZip(string path) =>
        File.Exists(path) && new FileInfo(path).Length >= ZipMinBytes;

    private static async Task DownloadBuildAsync(
        string destination,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(60) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("FPS-Optimize-GOD-PC/1.0");

        using var response = await client
            .GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? 0;
        var tempPath = destination + ".download";
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var file = File.Create(tempPath);
        var buffer = new byte[131072];
        long read = 0;
        int bytes;
        var lastReport = 0;
        while ((bytes = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            await file.WriteAsync(buffer.AsMemory(0, bytes), cancellationToken).ConfigureAwait(false);
            read += bytes;
            if (total > 0)
            {
                var pct = (int)(read * 100 / total);
                if (pct != lastReport)
                {
                    lastReport = pct;
                    progress?.Report($"Downloading Spaceship Demo... {pct}%");
                }
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

        // The Unity player exe sits next to its "<name>_Data" folder. Promote that folder
        // (whatever nesting the zip used) to the install root so ResolveExecutable finds it.
        var exe = FindUnityExecutable(extractRoot);
        var sourceDir = exe is not null ? Path.GetDirectoryName(exe)! : extractRoot;
        CopyDirectory(sourceDir, destination);
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
