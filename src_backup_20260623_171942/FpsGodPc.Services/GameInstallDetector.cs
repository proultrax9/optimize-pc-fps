using System.Text.Json;

namespace FpsGodPc.Services;

public sealed class GameInstallDetector
{
    private readonly ProcessRunner _runner;

    public GameInstallDetector(ProcessRunner runner) => _runner = runner;

    public Dictionary<string, string> DetectPaths(IEnumerable<string> executables)
    {
        var targets = executables.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (targets.Count == 0)
        {
            return [];
        }

        var list = string.Join(",", targets.Select(e => $"'{e.Replace("'", "''")}'"));
        var script = $$"""
            $targets = @({{list}})
            $result = @{}
            $steamPath = (Get-ItemProperty 'HKCU:\Software\Valve\Steam' -ErrorAction SilentlyContinue).SteamPath
            foreach ($exe in $targets) {
              if (Test-Path $exe) { $result[$exe] = $exe; continue }
              $procName = $exe -replace '\.exe$',''
              $running = Get-Process -Name $procName -ErrorAction SilentlyContinue | Select-Object -First 1
              if ($running -and $running.Path) { $result[$exe] = $running.Path; continue }
              if ($steamPath) {
                $hit = Get-ChildItem (Join-Path $steamPath 'steamapps\common') -Recurse -Filter (Split-Path $exe -Leaf) -ErrorAction SilentlyContinue | Select-Object -First 1
                if ($hit) { $result[$exe] = $hit.FullName; continue }
              }
            }
            $result | ConvertTo-Json -Compress
            """;

        try
        {
            var raw = _runner.RunPowerShell(script);
            var line = raw.Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.StartsWith('{'));
            if (line is null)
            {
                return [];
            }

            return JsonSerializer.Deserialize<Dictionary<string, string>>(line) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
