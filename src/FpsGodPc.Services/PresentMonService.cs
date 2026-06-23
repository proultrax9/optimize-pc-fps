using System.Diagnostics;
using System.Globalization;

namespace FpsGodPc.Services;

public sealed class PresentMonStatus
{
    public bool Available { get; init; }
    public string? Path { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed class PresentMonService
{
    private readonly ProcessRunner _runner;

    public PresentMonService(ProcessRunner runner)
    {
        _runner = runner;
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

    public BenchmarkSession? TryCapture(string label, uint durationSecs, string presentMonPath, string benchmarkDir)
    {
        try
        {
            Directory.CreateDirectory(benchmarkDir);
            var csvPath = Path.Combine(benchmarkDir, $"pm-{DateTimeOffset.Now.ToUnixTimeSeconds()}.csv");
            _runner.RunCommand(
                presentMonPath,
                "-output_file", csvPath,
                "-terminate_after_timed", durationSecs.ToString(),
                "-timed", durationSecs.ToString(),
                "-no_top");

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
        var frametimes = new List<float>();
        string? header = null;
        var lineIndex = 0;
        foreach (var line in File.ReadLines(csvPath))
        {
            if (lineIndex++ == 0)
            {
                header = line;
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var cols = line.Split(',');
            var msCol = FindMsBetweenPresentsIndex(header) ?? 7;
            if (cols.Length > msCol &&
                float.TryParse(cols[msCol].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var ms) &&
                ms > 0f)
            {
                frametimes.Add(ms);
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

    private static int? FindMsBetweenPresentsIndex(string? headerLine)
    {
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            return null;
        }

        var headers = headerLine.Split(',');
        for (var i = 0; i < headers.Length; i++)
        {
            if (headers[i].Contains("MsBetweenPresents", StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return null;
    }
}
