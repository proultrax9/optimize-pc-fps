using System.Diagnostics;

namespace FpsGodPc.Services;

public sealed class ElevationHelper
{
    private readonly ProcessRunner _runner;

    public ElevationHelper(ProcessRunner runner) => _runner = runner;

    public bool IsElevated()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            var result = _runner.RunPowerShell(
                "([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)");
            return result.Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public void RestartElevated()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Windows only.");
        }

        var exe = Process.GetCurrentProcess().MainModule?.FileName
            ?? Environment.ProcessPath
            ?? throw new InvalidOperationException("Cannot resolve executable path.");

        var escaped = exe.Replace("'", "''");
        _runner.RunPowerShell($"Start-Process -FilePath '{escaped}' -Verb RunAs");
    }
}
