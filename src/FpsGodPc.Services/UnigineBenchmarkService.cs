using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace FpsGodPc.Services;

public sealed class UnigineBenchmarkStatus
{
    public bool Available { get; init; }
    public string? ExecutablePath { get; init; }
    public string Engine { get; init; } = "none";
    public string Message { get; init; } = string.Empty;
    public string InstallFolder { get; init; } = string.Empty;
}

public sealed class UnigineBenchmarkSession
{
    public Process Process { get; init; } = null!;
    public string ExecutablePath { get; init; } = string.Empty;
    public string WorkDir { get; init; } = string.Empty;
    public string Engine { get; init; } = "Heaven";
    public DateTime StartedAtUtc { get; init; }
    public uint DurationSecs { get; init; }
    public string ReportCsvPath { get; init; } = string.Empty;
    public string? PresentMonCsvPath { get; set; }
    public Process? PresentMonProcess { get; set; }
}

public sealed class UnigineBenchmarkRunResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Engine { get; init; } = "Unigine";
    public float? AvgFps { get; init; }
    public float? MinFps { get; init; }
    public float? MaxFps { get; init; }
    public uint DurationSecs { get; init; }
    public string ScoreSource { get; init; } = "Heaven";
}

public sealed class UnigineBenchmarkService
{
    private readonly PresentMonService _presentMon;

    public UnigineBenchmarkService(PresentMonService presentMon)
    {
        _presentMon = presentMon;
    }

    private static readonly string[] ExecutableNames =
    [
        "heaven_x64.exe",
        "Heaven.exe",
        "heaven.exe",
        "Unigine Heaven Benchmark.exe",
        "superposition.exe",
        "Superposition.exe",
    ];

    private static readonly string[] RelativeExecutablePaths =
    [
        @"bin\heaven_x64.exe",
        @"bin\Heaven.exe",
        @"bin\heaven.exe",
        @"Heaven.exe",
        @"heaven.exe",
        @"superposition.exe",
        @"Superposition.exe",
    ];

    private const string HeavenInstallerUrl = "https://assets.unigine.com/d/Unigine_Heaven-4.0.exe";
    private const long HeavenInstallerMinBytes = 200_000_000;

    private static readonly Regex AvgFpsPattern = new(
        @"(?:Average\s+FPS|FPS)\s*[:\s]+([\d]+(?:\.[\d]+)?)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex MinFpsPattern = new(
        @"Min(?:imum)?\s+FPS\s*[:\s]+([\d]+(?:\.[\d]+)?)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex MaxFpsPattern = new(
        @"Max(?:imum)?\s+FPS\s*[:\s]+([\d]+(?:\.[\d]+)?)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex HeavenHtmlFpsPattern = new(
        @"(?:Average\s+FPS|>FPS<|FPS\s*</(?:td|span|div)>)\s*(?:</(?:td|span|div)>\s*<(?:td|span|div)[^>]*>)?\s*([\d]+(?:\.[\d]+)?)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex HeavenHtmlMinFpsPattern = new(
        @"Min(?:imum)?\s+FPS\s*(?:</(?:td|span|div)>\s*<(?:td|span|div)[^>]*>|:)\s*([\d]+(?:\.[\d]+)?)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex HeavenHtmlMaxFpsPattern = new(
        @"Max(?:imum)?\s+FPS\s*(?:</(?:td|span|div)>\s*<(?:td|span|div)[^>]*>|:)\s*([\d]+(?:\.[\d]+)?)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static string[] GetProcessNamesForCapture(string exe)
    {
        var name = Path.GetFileNameWithoutExtension(exe);
        if (name.Equals("heaven_x64", StringComparison.OrdinalIgnoreCase))
        {
            return ["heaven_x64", "Heaven"];
        }

        return [name];
    }

    private string? _cachedExecutable;

    public void InvalidateCache() => _cachedExecutable = null;

    public UnigineBenchmarkStatus GetStatus()
    {
        var folder = GetBenchmarkFolder();
        var exe = ResolveExecutable();
        if (exe is null)
        {
            return new UnigineBenchmarkStatus
            {
                Available = false,
                InstallFolder = folder,
                Message = "Heaven not installed yet — click Launch to download and start the 3D test.",
            };
        }

        var engine = Path.GetFileName(exe).Contains("superposition", StringComparison.OrdinalIgnoreCase)
            ? "Superposition"
            : "Heaven";

        return new UnigineBenchmarkStatus
        {
            Available = true,
            ExecutablePath = exe,
            Engine = engine,
            InstallFolder = folder,
            Message = $"{engine} ready — click Launch to start the 3D fly-through test.",
        };
    }

    public async Task<(bool Success, string Message)> EnsureInstalledAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (ResolveExecutable() is not null)
        {
            return (true, GetStatus().Message);
        }

        var folder = GetBenchmarkFolder();
        Directory.CreateDirectory(folder);
        var installDir = Path.Combine(folder, "Heaven Benchmark 4.0");
        var installerPath = Path.Combine(folder, "Unigine_Heaven-4.0.exe");

        try
        {
            if (!IsValidInstaller(installerPath))
            {
                progress?.Report("Downloading Unigine Heaven (~247 MB)...");
                await DownloadInstallerAsync(installerPath, progress, cancellationToken).ConfigureAwait(false);
            }

            progress?.Report("Installing Heaven benchmark...");
            Directory.CreateDirectory(installDir);
            await RunInstallerAsync(installerPath, installDir, cancellationToken).ConfigureAwait(false);
            InvalidateCache();

            for (var i = 0; i < 45; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (ResolveExecutable(forceRefresh: true) is not null)
                {
                    return (true, "Heaven benchmark installed — ready to launch.");
                }

                await Task.Delay(2000, cancellationToken).ConfigureAwait(false);
            }

            return (false, "Install finished but heaven.exe was not found. Restart the app and try again.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return (false, $"Failed to install Heaven: {ex.Message}");
        }
    }

    public (bool Success, string Message, UnigineBenchmarkSession? Session) StartBenchmark(uint durationSeconds)
    {
        durationSeconds = Math.Clamp(durationSeconds, 10u, 600u);
        var exe = ResolveExecutable();
        if (exe is null)
        {
            return (false, "Heaven benchmark is not installed.", null);
        }

        var workDir = Path.GetDirectoryName(exe)!;
        var engine = Path.GetFileName(exe).Contains("superposition", StringComparison.OrdinalIgnoreCase)
            ? "Superposition"
            : "Heaven";
        var startedAt = DateTime.UtcNow;
        var reportCsv = CreateReportPath(startedAt);
        ApplyFullscreenVideoSettings(exe);
        var args = BuildArguments(exe, durationSeconds, reportCsv);

        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                WorkingDirectory = workDir,
                UseShellExecute = true,
            });

            if (process is null)
            {
                return (false, "Failed to start the 3D benchmark process.", null);
            }

            return (true, "Heaven launched — 1-minute benchmark will run automatically.", new UnigineBenchmarkSession
            {
                Process = process,
                ExecutablePath = exe,
                WorkDir = workDir,
                Engine = engine,
                StartedAtUtc = startedAt,
                DurationSecs = durationSeconds,
                ReportCsvPath = reportCsv,
            });
        }
        catch (Exception ex)
        {
            return (false, $"Failed to launch benchmark: {ex.Message}", null);
        }
    }

    public async Task<UnigineBenchmarkRunResult> RunAutomatedBenchmarkAsync(
        uint durationSeconds,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        durationSeconds = Math.Clamp(durationSeconds, 10u, 600u);
        await _presentMon.EnsureInstalledAsync(progress, cancellationToken).ConfigureAwait(false);

        var (success, message, session) = StartBenchmark(durationSeconds);
        if (!success || session is null)
        {
            return new UnigineBenchmarkRunResult
            {
                Success = false,
                Message = message,
                DurationSecs = durationSeconds,
            };
        }

        try
        {
            progress?.Report("Loading Heaven 3D world...");
            if (!await HeavenAutomation.WaitForMainWindowAsync(session.Process, TimeSpan.FromSeconds(60), cancellationToken)
                    .ConfigureAwait(false))
            {
                HeavenAutomation.KillProcessTree(session.Process);
                return new UnigineBenchmarkRunResult
                {
                    Success = false,
                    Message = "Heaven did not open. Try again or restart your PC.",
                    Engine = session.Engine,
                    DurationSecs = durationSeconds,
                };
            }

            await Task.Delay(10_000, cancellationToken).ConfigureAwait(false);
            progress?.Report($"Running {durationSeconds}s fly-through benchmark...");
            HeavenAutomation.FocusAndStartBenchmark(session.Process);
            await Task.Delay(1500, cancellationToken).ConfigureAwait(false);

            var presentMonCsv = Path.Combine(GetReportsFolder(), $"presentmon-{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
            var processNames = GetProcessNamesForCapture(session.ExecutablePath);
            session.PresentMonCsvPath = presentMonCsv;
            session.PresentMonProcess = _presentMon.StartTimedCapture(durationSeconds + 10, presentMonCsv, processNames);

            await Task.Delay(TimeSpan.FromSeconds(durationSeconds), cancellationToken).ConfigureAwait(false);

            progress?.Report("Saving benchmark results...");
            HeavenAutomation.KillProcessTree(session.Process);
            WaitForPresentMon(session);
            await Task.Delay(2000, cancellationToken).ConfigureAwait(false);

            return CompleteBenchmark(session);
        }
        catch (OperationCanceledException)
        {
            HeavenAutomation.KillProcessTree(session.Process);
            TryStopPresentMon(session.PresentMonProcess);
            throw;
        }
        catch (Exception ex)
        {
            HeavenAutomation.KillProcessTree(session.Process);
            TryStopPresentMon(session.PresentMonProcess);
            return new UnigineBenchmarkRunResult
            {
                Success = false,
                Engine = session.Engine,
                DurationSecs = durationSeconds,
                Message = $"Benchmark failed: {ex.Message}",
            };
        }
    }

    public UnigineBenchmarkRunResult CompleteBenchmark(UnigineBenchmarkSession session)
    {
        try
        {
            if (!session.Process.HasExited)
            {
                var timeoutMs = (int)(session.DurationSecs * 1000 + 180_000);
                if (!session.Process.WaitForExit(timeoutMs))
                {
                    try
                    {
                        session.Process.Kill(entireProcessTree: true);
                    }
                    catch
                    {
                        // Best effort.
                    }

                    return new UnigineBenchmarkRunResult
                    {
                        Success = false,
                        Engine = session.Engine,
                        DurationSecs = session.DurationSecs,
                        Message = "Benchmark timed out. Close Heaven manually and try again.",
                    };
                }
            }
        }
        catch (Exception ex)
        {
            return new UnigineBenchmarkRunResult
            {
                Success = false,
                Engine = session.Engine,
                DurationSecs = session.DurationSecs,
                Message = $"Benchmark wait failed: {ex.Message}",
            };
        }

        WaitForPresentMon(session);

        // Heaven Basic writes HTML reports after the process exits; allow a moment for flush.
        Thread.Sleep(2000);

        var scores = ParseResults(session);
        if (scores.AvgFps is null)
        {
            return new UnigineBenchmarkRunResult
            {
                Success = true,
                Engine = session.Engine,
                DurationSecs = session.DurationSecs,
                ScoreSource = session.Engine,
                Message = "Benchmark finished but FPS was not captured. Run as Administrator and try again.",
            };
        }

        return new UnigineBenchmarkRunResult
        {
            Success = true,
            Engine = session.Engine,
            DurationSecs = session.DurationSecs,
            AvgFps = scores.AvgFps,
            MinFps = scores.MinFps,
            MaxFps = scores.MaxFps,
            ScoreSource = scores.Source,
            Message = $"Done — {scores.AvgFps:F1} FPS average over {session.DurationSecs}s ({scores.Source}).",
        };
    }

    public static string GetBenchmarkFolder() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "fps-god-pc",
            "benchmark");

    public static string GetReportsFolder()
    {
        var folder = Path.Combine(GetBenchmarkFolder(), "reports");
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static string CreateReportPath(DateTime startedAtUtc) =>
        Path.Combine(GetReportsFolder(), $"heaven-{startedAtUtc:yyyyMMdd_HHmmss}.csv");

    private static string BuildArguments(string exe, uint durationSeconds, string reportCsv)
    {
        var (width, height) = GetPrimaryScreenSize();

        if (exe.Contains("superposition", StringComparison.OrdinalIgnoreCase))
        {
            return string.Join(' ',
                "-benchmark 1",
                $"-time_benchmark {durationSeconds}",
                "-fullscreen 1",
                "-video_fullscreen 1",
                "-video_mode -1",
                $"-video_width {width}",
                $"-video_height {height}",
                $"-log \"{reportCsv}\"");
        }

        return string.Join(' ',
            "-data_path ../",
            "-sound_app null",
            "-system_script heaven/unigine.cpp",
            "-engine_config ../data/heaven_4.0.cfg",
            "-benchmark Heaven",
            $"-time_script {durationSeconds}",
            "-loop 1",
            "-video_app direct3d11",
            "-video_mode -1",
            "-video_fullscreen 1",
            "-fullscreen 1",
            $"-video_width {width}",
            $"-video_height {height}",
            $"-log \"{reportCsv}\"",
            "-log_format \"$F,$z,$x,$S\"");
    }

    private static void ApplyFullscreenVideoSettings(string exePath)
    {
        var workDir = Path.GetDirectoryName(exePath)!;
        var cfgPath = Path.Combine(workDir, "unigine.cfg");
        if (!File.Exists(cfgPath))
        {
            return;
        }

        var (width, height) = GetPrimaryScreenSize();
        try
        {
            var content = File.ReadAllText(cfgPath);
            content = SetCfgInt(content, "video_fullscreen", 1);
            content = SetCfgInt(content, "video_mode", -1);
            content = SetCfgInt(content, "video_width", width);
            content = SetCfgInt(content, "video_height", height);
            content = SetCfgInt(content, "video_resizable", 0);
            content = SetCfgItem(content, "video_app", "direct3d11");
            content = SetCfgItem(content, "system_script", "heaven/unigine.cpp");
            File.WriteAllText(cfgPath, content);
        }
        catch
        {
            // Best effort — command-line flags still request fullscreen.
        }
    }

    private static string SetCfgInt(string xml, string name, int value)
    {
        var pattern = $@"<item name=""{Regex.Escape(name)}"" type=""int"">\d+</item>";
        var replacement = $@"<item name=""{name}"" type=""int"">{value}</item>";
        return Regex.IsMatch(xml, pattern)
            ? Regex.Replace(xml, pattern, replacement)
            : xml;
    }

    private static string SetCfgItem(string xml, string name, string value)
    {
        var pattern = $@"<item name=""{Regex.Escape(name)}"" type=""string"">[^<]*</item>";
        var replacement = $@"<item name=""{name}"" type=""string"">{value}</item>";
        return Regex.IsMatch(xml, pattern)
            ? Regex.Replace(xml, pattern, replacement)
            : xml;
    }

    private static (int Width, int Height) GetPrimaryScreenSize()
    {
        const int fallbackWidth = 1920;
        const int fallbackHeight = 1080;
        try
        {
            var width = GetSystemMetrics(SmCxScreen);
            var height = GetSystemMetrics(SmCyScreen);
            if (width > 0 && height > 0)
            {
                return (width, height);
            }
        }
        catch
        {
            // Fall back to a common desktop resolution.
        }

        return (fallbackWidth, fallbackHeight);
    }

    private const int SmCxScreen = 0;
    private const int SmCyScreen = 1;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private static void WaitForPresentMon(UnigineBenchmarkSession session)
    {
        if (session.PresentMonProcess is null)
        {
            return;
        }

        try
        {
            if (!session.PresentMonProcess.HasExited)
            {
                session.PresentMonProcess.WaitForExit(120_000);
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

    private static bool IsValidInstaller(string path) =>
        File.Exists(path) && new FileInfo(path).Length >= HeavenInstallerMinBytes;

    private static async Task DownloadInstallerAsync(
        string destination,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(45) };
        using var response = await client
            .GetAsync(HeavenInstallerUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? 0;
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var file = File.Create(destination);
        var buffer = new byte[81920];
        long read = 0;
        int bytes;
        while ((bytes = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            await file.WriteAsync(buffer.AsMemory(0, bytes), cancellationToken).ConfigureAwait(false);
            read += bytes;
            if (total > 0)
            {
                progress?.Report($"Downloading Heaven... {read * 100 / total}%");
            }
        }
    }

    private static Task RunInstallerAsync(string installerPath, string installDir, CancellationToken cancellationToken) =>
        Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = $"/DIR=\"{installDir}\" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART",
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            process?.WaitForExit(600_000);
        }, cancellationToken);

    private sealed record BenchmarkScores(float? AvgFps, float? MinFps, float? MaxFps, string Source);

    private BenchmarkScores ParseResults(UnigineBenchmarkSession session)
    {
        if (!string.IsNullOrWhiteSpace(session.PresentMonCsvPath))
        {
            var pm = _presentMon.TryParseCsvFile(session.PresentMonCsvPath);
            if (pm?.AvgFps is float avg)
            {
                return new BenchmarkScores(avg, pm.MinFps, avg, "presentmon");
            }
        }

        if (!string.IsNullOrWhiteSpace(session.ReportCsvPath) && File.Exists(session.ReportCsvPath))
        {
            var fromReport = ParseHeavenCsv(session.ReportCsvPath);
            if (fromReport.AvgFps is not null)
            {
                return new BenchmarkScores(fromReport.AvgFps, fromReport.MinFps, fromReport.MaxFps, session.Engine);
            }
        }

        foreach (var dir in CollectResultDirectories(session.WorkDir))
        {
            var newestCsv = FindNewestFile(dir, "*.csv", session.StartedAtUtc);
            if (newestCsv is null)
            {
                continue;
            }

            var fromCsv = ParseHeavenCsv(newestCsv);
            if (fromCsv.AvgFps is not null)
            {
                return new BenchmarkScores(fromCsv.AvgFps, fromCsv.MinFps, fromCsv.MaxFps, session.Engine);
            }
        }

        var newestHtml = CollectResultDirectories(session.WorkDir)
            .Select(dir => FindNewestFile(dir, "*.htm*", session.StartedAtUtc))
            .Where(path => path is not null)
            .OrderByDescending(path => new FileInfo(path!).LastWriteTimeUtc)
            .FirstOrDefault();
        if (newestHtml is not null)
        {
            var fromHtml = ParseHeavenHtml(newestHtml);
            if (fromHtml.AvgFps is not null)
            {
                return new BenchmarkScores(fromHtml.AvgFps, fromHtml.MinFps, fromHtml.MaxFps, session.Engine);
            }
        }

        var textScores = ParseTextResults(session.WorkDir, session.StartedAtUtc);
        if (textScores.AvgFps is not null)
        {
            return new BenchmarkScores(textScores.AvgFps, textScores.MinFps, textScores.MaxFps, session.Engine);
        }

        return new BenchmarkScores(null, null, null, session.Engine);
    }

    private static string? FindNewestFile(string dir, string pattern, DateTime startedAtUtc)
    {
        if (!Directory.Exists(dir))
        {
            return null;
        }

        try
        {
            var searchDepth = dir.Contains("Heaven", StringComparison.OrdinalIgnoreCase)
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            return Directory.EnumerateFiles(dir, pattern, searchDepth)
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

    private static (float? AvgFps, float? MinFps, float? MaxFps) ParseHeavenHtml(string htmlPath)
    {
        try
        {
            var text = File.ReadAllText(htmlPath);
            var avg = MatchFloat(HeavenHtmlFpsPattern, text) ?? MatchFloat(AvgFpsPattern, text);
            if (avg is null)
            {
                return (null, null, null);
            }

            return (
                avg,
                MatchFloat(HeavenHtmlMinFpsPattern, text) ?? MatchFloat(MinFpsPattern, text),
                MatchFloat(HeavenHtmlMaxFpsPattern, text) ?? MatchFloat(MaxFpsPattern, text));
        }
        catch
        {
            return (null, null, null);
        }
    }

    private static (float? AvgFps, float? MinFps, float? MaxFps) ParseHeavenCsv(string csvPath)
    {
        try
        {
            var lines = File.ReadAllLines(csvPath)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();
            if (lines.Count == 0)
            {
                return (null, null, null);
            }

            for (var i = lines.Count - 1; i >= 0; i--)
            {
                var cols = lines[i].Split(',');
                if (cols.Length == 0)
                {
                    continue;
                }

                if (cols[0].Contains("FPS", StringComparison.OrdinalIgnoreCase) ||
                    cols[0].Contains("Score", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (TryParseFloat(cols[0], out var avg) && avg is > 0f and < 20_000f)
                {
                    float? min = cols.Length > 1 && TryParseFloat(cols[1], out var minVal) ? minVal : null;
                    float? max = cols.Length > 2 && TryParseFloat(cols[2], out var maxVal) ? maxVal : null;
                    return (avg, min, max);
                }
            }
        }
        catch
        {
            // Ignore parse errors.
        }

        return (null, null, null);
    }

    private static bool TryParseFloat(string value, out float result) =>
        float.TryParse(value.Trim().Trim('"'), NumberStyles.Float, CultureInfo.InvariantCulture, out result);

    private static (float? AvgFps, float? MinFps, float? MaxFps) ParseTextResults(string workDir, DateTime startedAtUtc)
    {
        var searchDirs = CollectResultDirectories(workDir);
        var candidates = new List<FileInfo>();

        foreach (var dir in searchDirs)
        {
            if (!Directory.Exists(dir))
            {
                continue;
            }

            try
            {
                foreach (var pattern in new[] { "*.html", "*.htm", "*.log", "*.txt" })
                {
                    foreach (var path in Directory.EnumerateFiles(dir, pattern, SearchOption.AllDirectories))
                    {
                        var info = new FileInfo(path);
                        if (info.LastWriteTimeUtc >= startedAtUtc.AddSeconds(-5))
                        {
                            candidates.Add(info);
                        }
                    }
                }
            }
            catch
            {
                // Ignore unreadable folders.
            }
        }

        foreach (var file in candidates.OrderByDescending(f => f.LastWriteTimeUtc))
        {
            string text;
            try
            {
                text = File.ReadAllText(file.FullName);
            }
            catch
            {
                continue;
            }

            var avg = MatchFloat(AvgFpsPattern, text);
            if (avg is null)
            {
                continue;
            }

            return (avg, MatchFloat(MinFpsPattern, text), MatchFloat(MaxFpsPattern, text));
        }

        return (null, null, null);
    }

    private static List<string> CollectResultDirectories(string workDir)
    {
        var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var installRoot = Directory.GetParent(workDir)?.FullName;
        return
        [
            GetReportsFolder(),
            Path.Combine(profile, "Heaven", "reports"),
            Path.Combine(docs, "Heaven", "reports"),
            workDir,
            installRoot ?? workDir,
            GetBenchmarkFolder(),
            Path.Combine(GetBenchmarkFolder(), "Heaven Benchmark 4.0"),
            Path.Combine(docs, "Unigine Heaven"),
            Path.Combine(docs, "Unigine"),
        ];
    }

    private static float? MatchFloat(Regex pattern, string text)
    {
        var match = pattern.Match(text);
        if (!match.Success)
        {
            return null;
        }

        return float.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private string? ResolveExecutable(bool forceRefresh = false)
    {
        if (!forceRefresh && _cachedExecutable is not null && File.Exists(_cachedExecutable))
        {
            return _cachedExecutable;
        }

        _cachedExecutable = FindExecutable();
        return _cachedExecutable;
    }

    private static string? FindExecutable()
    {
        foreach (var root in GetSearchRoots())
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var rel in RelativeExecutablePaths)
            {
                var path = Path.Combine(root, rel);
                if (File.Exists(path))
                {
                    return path;
                }
            }

            foreach (var name in ExecutableNames)
            {
                var path = Path.Combine(root, name);
                if (File.Exists(path))
                {
                    return path;
                }
            }
        }

        return null;
    }

    private static IEnumerable<string> GetSearchRoots()
    {
        var folder = GetBenchmarkFolder();
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        yield return Path.Combine(folder, "Heaven Benchmark 4.0");
        yield return folder;
        yield return Path.Combine(programFilesX86, "Unigine", "Heaven Benchmark 4.0");
        yield return Path.Combine(programFiles, "Unigine", "Heaven Benchmark 4.0");
        yield return Path.Combine(programFilesX86, "Unigine", "Heaven Benchmark");
        yield return Path.Combine(programFiles, "Unigine", "Heaven Benchmark");
        yield return Path.Combine(programFilesX86, "Unigine", "Superposition");
        yield return Path.Combine(programFiles, "Unigine", "Superposition");
    }
}
