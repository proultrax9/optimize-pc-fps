using System.Diagnostics;
using System.Globalization;

namespace FpsGodPc.Services;

public sealed class FpsCaptureStats
{
    public float? AvgFps { get; init; }
    public float? MinFps { get; init; }
    public float? Pct1Low { get; init; }
    public float? AvgFrametimeMs { get; init; }
    public string Source { get; init; } = string.Empty;
}

public sealed class PresentMonStatus
{
    public bool Available { get; init; }
    public string? Path { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed class PresentMonService
{
    private const string PresentMonDownloadUrl =
        "https://github.com/GameTechDev/PresentMon/releases/download/v2.4.1/PresentMon-2.4.1-x64.exe";

    private const long PresentMonMinBytes = 400_000;

    private readonly ProcessRunner _runner;

    public PresentMonService(ProcessRunner runner)
    {
        _runner = runner;
    }

    public async Task<(bool Success, string Message)> EnsureInstalledAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (FindPresentMon() is not null)
        {
            return (true, GetStatus().Message);
        }

        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "fps-god-pc");
        Directory.CreateDirectory(folder);
        var destination = Path.Combine(folder, "PresentMon.exe");
        var tempPath = destination + ".download";

        try
        {
            progress?.Report("Downloading PresentMon for FPS capture...");
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            using var response = await client
                .GetAsync(PresentMonDownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using var file = File.Create(tempPath);
            await stream.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
            file.Close();

            if (new FileInfo(tempPath).Length < PresentMonMinBytes)
            {
                return (false, "PresentMon download was incomplete. Check your internet connection and try again.");
            }

            if (File.Exists(destination))
            {
                File.Delete(destination);
            }

            File.Move(tempPath, destination);
            return (true, "PresentMon installed — FPS will be captured during benchmarks.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return (false, $"Failed to download PresentMon: {ex.Message}");
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
                // Best effort.
            }
        }
    }

    public PresentMonStatus GetStatus()
    {
        var path = FindPresentMon();
        return path is null
            ? new PresentMonStatus
            {
                Available = false,
                Message = "PresentMon not found. Place PresentMon.exe in %LOCALAPPDATA%\\fps-god-pc\\ or PATH. Telemetry fallback will be used."
            }
            : new PresentMonStatus
            {
                Available = true,
                Path = path,
                Message = "PresentMon detected — FPS metrics available."
            };
    }

    public FpsCaptureStats? TryParseCsvFile(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            return null;
        }

        var stats = ParseCsv(csvPath);
        if (stats.AvgFps is null)
        {
            return null;
        }

        return new FpsCaptureStats
        {
            AvgFps = stats.AvgFps,
            MinFps = stats.Pct1Low,
            Pct1Low = stats.Pct1Low,
            AvgFrametimeMs = stats.AvgFrametimeMs,
            Source = "presentmon",
        };
    }

    public Process? StartTimedCapture(uint durationSecs, string outputCsvPath, params string[] processNames)
    {
        var presentMonPath = FindPresentMon();
        if (presentMonPath is null || processNames.Length == 0)
        {
            return null;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputCsvPath)!);
            var nameArgs = string.Join(' ', processNames.Select(n => $"--process_name {n}"));
            return Process.Start(new ProcessStartInfo
            {
                FileName = presentMonPath,
                Arguments =
                    $"--output_file \"{outputCsvPath}\" {nameArgs} --timed {durationSecs} --terminate_after_timed --no_console_stats",
                UseShellExecute = false,
                CreateNoWindow = true,
            });
        }
        catch
        {
            return null;
        }
    }

    public Process? StartProcessCapture(string outputCsvPath, params string[] processNames)
    {
        var presentMonPath = FindPresentMon();
        if (presentMonPath is null || processNames.Length == 0)
        {
            return null;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputCsvPath)!);
            var nameArgs = string.Join(' ', processNames.Select(n => $"--process_name {n}"));
            return Process.Start(new ProcessStartInfo
            {
                FileName = presentMonPath,
                Arguments =
                    $"--output_file \"{outputCsvPath}\" {nameArgs} --terminate_on_proc_exit --no_console_stats",
                UseShellExecute = false,
                CreateNoWindow = true,
            });
        }
        catch
        {
            return null;
        }
    }

    public BenchmarkSession? TryCapture(string label, uint durationSecs, string presentMonPath, string benchmarkDir)
    {
        try
        {
            Directory.CreateDirectory(benchmarkDir);
            var csvPath = Path.Combine(benchmarkDir, $"pm-{DateTimeOffset.Now.ToUnixTimeSeconds()}.csv");
            _runner.RunCommand(
                presentMonPath,
                "--output_file", csvPath,
                "--timed", durationSecs.ToString(),
                "--terminate_after_timed",
                "--no_console_stats");

            if (!File.Exists(csvPath))
            {
                return null;
            }

            var stats = ParseCsv(csvPath);
            return new BenchmarkSession
            {
                Label = label,
                TakenAt = DateTimeOffset.Now.ToString("s"),
                DurationSecs = durationSecs,
                AvgFps = stats.AvgFps,
                Pct1Low = stats.Pct1Low,
                Pct01Low = stats.Pct01Low,
                AvgFrametimeMs = stats.AvgFrametimeMs,
                Source = "presentmon",
            };
        }
        catch
        {
            return null;
        }
    }

    public static string? FindPresentMon()
    {
        var local = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "fps-god-pc",
            "PresentMon.exe");
        if (File.Exists(local))
        {
            return local;
        }

        var appDir = Path.Combine(AppContext.BaseDirectory, "PresentMon.exe");
        if (File.Exists(appDir))
        {
            return appDir;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "where",
                Arguments = "PresentMon.exe",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
            };
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            var first = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return first is not null && File.Exists(first) ? first : null;
        }
        catch
        {
            return null;
        }
    }

    private static (float? AvgFps, float? Pct1Low, float? Pct01Low, float? AvgFrametimeMs) ParseCsv(string csvPath)
    {
        string? header = null;
        var columnIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var frametimes = new List<float>();

        foreach (var column in new[]
                 {
                     "MsBetweenPresents",
                     "MsUntilDisplayed",
                     "MsBetweenDisplayChange",
                     "MsGPUTime",
                 })
        {
            frametimes.Clear();
            columnIndexes.Clear();
            header = null;

            var lineIndex = 0;
            foreach (var line in File.ReadLines(csvPath))
            {
                if (lineIndex++ == 0)
                {
                    header = line;
                    var headers = ParseCsvLine(line);
                    for (var i = 0; i < headers.Count; i++)
                    {
                        columnIndexes[headers[i].Trim()] = i;
                    }

                    continue;
                }

                if (string.IsNullOrWhiteSpace(line) || header is null)
                {
                    continue;
                }

                if (!columnIndexes.TryGetValue(column, out var colIdx))
                {
                    break;
                }

                var cols = ParseCsvLine(line);
                if (cols.Count <= colIdx)
                {
                    continue;
                }

                var raw = cols[colIdx].Trim();
                if (raw.Equals("NA", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var ms) || ms < 2f || ms > 50f)
                {
                    continue;
                }

                frametimes.Add(ms);
            }

            if (frametimes.Count >= 20)
            {
                break;
            }
        }

        if (frametimes.Count == 0)
        {
            return (null, null, null, null);
        }

        var fpsValues = frametimes.Select(ms => 1000f / ms).OrderBy(f => f).ToList();
        var avgFps = fpsValues.Average();
        var p1Idx = (int)Math.Floor(fpsValues.Count * 0.01);
        var p01Idx = (int)Math.Floor(fpsValues.Count * 0.001);
        p1Idx = Math.Clamp(p1Idx, 0, fpsValues.Count - 1);
        p01Idx = Math.Clamp(p01Idx, 0, fpsValues.Count - 1);
        var avgMs = frametimes.Average();
        return (avgFps, fpsValues[p1Idx], fpsValues[p01Idx], avgMs);
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        result.Add(current.ToString());
        return result;
    }
}
