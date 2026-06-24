using System.Diagnostics;
using System.Text;

namespace FpsGodPc.Services;

public sealed class ProcessRunner
{
    private const int DefaultTimeoutMs = 60_000;

    public string RunPowerShell(string script) =>
        RunCommand("powershell", "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-Command", script);

    public string RunCommand(string program, params string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = program,
            Arguments = string.Join(" ", args.Select(QuoteArg)),
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException($"Failed to start process: {program}");
        }

        // Read stdout and stderr concurrently to prevent deadlock when either
        // pipe buffer fills while we are blocking on the other pipe.
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        // Wait for both reads to finish, with an overall timeout.
        var bothCompleted = Task.WhenAll(stdoutTask, stderrTask)
            .Wait(DefaultTimeoutMs);

        if (!bothCompleted)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best-effort */ }
            throw new InvalidOperationException(
                $"Process '{program}' did not complete within {DefaultTimeoutMs / 1000}s and was killed.");
        }

        process.WaitForExit();

        var stdout = stdoutTask.Result;
        var stderr = stderrTask.Result;

        if (process.ExitCode == 0)
        {
            return string.IsNullOrWhiteSpace(stdout) ? stderr.Trim() : stdout.Trim();
        }

        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(stderr)
                ? $"Command failed ({process.ExitCode}): {stdout.Trim()}"
                : $"Command failed ({process.ExitCode}): {stderr.Trim()}");
    }

    private static string QuoteArg(string arg)
    {
        if (arg.Contains(' ') || arg.Contains('"'))
        {
            return $"\"{arg.Replace("\"", "\\\"")}\"";
        }

        return arg;
    }
}
