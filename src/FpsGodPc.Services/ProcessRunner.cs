using System.Diagnostics;
using System.Text;

namespace FpsGodPc.Services;

public sealed class ProcessRunner
{
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

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

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
