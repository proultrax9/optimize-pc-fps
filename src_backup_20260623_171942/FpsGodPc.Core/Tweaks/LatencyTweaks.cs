using FpsGodPc.Services;

namespace FpsGodPc.Core.Tweaks;

internal static class LatencyTweaks
{
    public static string? ReadPrioritySeparation(ProcessRunner runner)
    {
        var outText = runner.RunCommand(
            "reg", "query", @"HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl", "/v", "Win32PrioritySeparation");
        foreach (var line in outText.Split('\n'))
        {
            if (!line.Contains("Win32PrioritySeparation", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var idx = line.IndexOf("0x", StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                continue;
            }

            var hex = line[(idx + 2)..].Trim().Split(' ', '\t')[0];
            return $"0x{hex}";
        }

        return null;
    }

    public static string ApplyWinPriority(ProcessRunner runner, string preset = "26")
    {
        var value = preset switch
        {
            "28" => "0x28",
            "2a" => "0x2A",
            _ => "0x26",
        };

        runner.RunCommand(
            "reg", "add", @"HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl",
            "/v", "Win32PrioritySeparation", "/t", "REG_DWORD", "/d", value, "/f");
        return $"Win32PrioritySeparation set to {value} (gaming latency profile).";
    }

    public static string RevertWinPriority(ProcessRunner runner, string? backup)
    {
        var value = string.IsNullOrWhiteSpace(backup) ? "0x2" : backup;
        runner.RunCommand(
            "reg", "add", @"HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl",
            "/v", "Win32PrioritySeparation", "/t", "REG_DWORD", "/d", value, "/f");
        return "Win32PrioritySeparation restored.";
    }

    public static string ApplyMmcssLatency(ProcessRunner runner)
    {
        runner.RunPowerShell("""
            $base = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile'
            New-Item -Path $base -Force | Out-Null
            Set-ItemProperty -Path $base -Name 'SystemResponsiveness' -Value 0 -Type DWord -Force
            Set-ItemProperty -Path $base -Name 'NetworkThrottlingIndex' -Value 0xffffffff -Type DWord -Force -ErrorAction SilentlyContinue
            $games = Join-Path $base 'Tasks\Games'
            New-Item -Path $games -Force | Out-Null
            Set-ItemProperty -Path $games -Name 'GPU Priority' -Value 8 -Type DWord -Force
            Set-ItemProperty -Path $games -Name 'Priority' -Value 6 -Type DWord -Force
            Set-ItemProperty -Path $games -Name 'Scheduling Category' -Value 'High' -Type String -Force
            Set-ItemProperty -Path $games -Name 'SFIO Priority' -Value 'High' -Type String -Force
            Set-ItemProperty -Path $games -Name 'Background Only' -Value 'False' -Type String -Force
            Set-ItemProperty -Path $games -Name 'Clock Rate' -Value 10000 -Type DWord -Force
            Set-ItemProperty -Path $games -Name 'Latency Sensitive' -Value 'True' -Type String -Force
            Set-ItemProperty -Path $games -Name 'Lazy Mode' -Value 'True' -Type String -Force -ErrorAction SilentlyContinue
            Set-ItemProperty -Path $games -Name 'Lazy Mode Timeout' -Value 9999999 -Type DWord -Force -ErrorAction SilentlyContinue
            """);
        return "MMCSS gaming profile applied (System Responsiveness 0, Games task tuned).";
    }

    public static string RevertMmcss(ProcessRunner runner)
    {
        runner.RunPowerShell("""
            $base = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile'
            Set-ItemProperty -Path $base -Name 'SystemResponsiveness' -Value 20 -Type DWord -Force -ErrorAction SilentlyContinue
            """);
        return "MMCSS SystemResponsiveness restored to default (20).";
    }

    public static string ApplySystemIniFps(AppStateStore store, ProcessRunner runner)
    {
        var windir = Environment.GetEnvironmentVariable("WINDIR") ?? @"C:\Windows";
        var iniPath = Path.Combine(windir, "system.ini");
        var backupDir = Path.Combine(store.DataDirectory, "backups");
        Directory.CreateDirectory(backupDir);
        var backupPath = Path.Combine(backupDir, "system.ini.bak");

        if (File.Exists(iniPath) && !File.Exists(backupPath))
        {
            File.Copy(iniPath, backupPath, overwrite: false);
        }

        const string profile = """
            ; FPS Optimize GOD PC — latency profile [386Enh]
            [386Enh]
            MinTimeSlice=1
            AvgTimeSlice=1
            MaxTimeSlice=1
            WinTimeSlice=1,1
            NetAsyncTimeout=0
            SyncTimeDivisor=1
            TimeWindowMinutes=0
            Latency=1

            [drivers]
            wave=mmdrv.dll
            timer=timer.drv
            """;
        File.WriteAllText(iniPath, profile);
        return "system.ini latency profile applied (backup saved).";
    }

    public static string RevertSystemIni(AppStateStore store)
    {
        var windir = Environment.GetEnvironmentVariable("WINDIR") ?? @"C:\Windows";
        var iniPath = Path.Combine(windir, "system.ini");
        var backupPath = Path.Combine(store.DataDirectory, "backups", "system.ini.bak");

        if (!File.Exists(backupPath))
        {
            throw new InvalidOperationException("No system.ini backup found.");
        }

        File.Copy(backupPath, iniPath, overwrite: true);
        return "system.ini restored from backup.";
    }

    public static string ApplyDisablePowerSaving(ProcessRunner runner)
    {
        var settings = new (string Subgroup, string Setting, string Value)[]
        {
            ("54533251-82be-4824-96c1-47b60b740d00", "893cee8e-2bef-41e0-89c6-b55d0929964a", "100"),
            ("54533251-82be-4824-96c1-47b60b740d00", "bc5038f7-23e0-4960-96da-33abaf5935ec", "100"),
            ("2a737441-1930-4402-8d77-b2bebba308a3", "48e6e7a6-50f5-4782-a5d4-53bb8f07e7d0", "0"),
            ("5015d140-abb1-4453-9b36-a8ccc6dc5dfa", "ee12f906-d277-404b-b6da-e5fa1f576df5", "0"),
            ("4f971e90-ee38-47e2-96bc-df3a8b899b35", "7648efa3-dd9c-4e3e-b566-50f929386280", "0"),
            ("238c9fa8-0aad-41ed-83f4-97be242c8f20", "29f6c1db-86da-48c5-9fdb-f2b67b1f44da", "0"),
        };

        var applied = 0;
        var skipped = 0;
        foreach (var (sub, setting, value) in settings)
        {
            try
            {
                runner.RunCommand("powercfg", "-setacvalueindex", "SCHEME_CURRENT", sub, setting, value);
                try { runner.RunCommand("powercfg", "-setdcvalueindex", "SCHEME_CURRENT", sub, setting, value); } catch { }
                applied++;
            }
            catch
            {
                skipped++;
            }
        }

        try { runner.RunCommand("powercfg", "-setactive", "SCHEME_CURRENT"); } catch { }

        try
        {
            runner.RunPowerShell("""
                Get-NetAdapter -Physical | ForEach-Object {
                  Disable-NetAdapterPowerManagement -Name $_.Name -ErrorAction SilentlyContinue
                }
                """);
        }
        catch { }

        if (applied == 0)
        {
            throw new InvalidOperationException("Could not adjust power settings on the active plan.");
        }

        return $"Power saving reduced ({applied} settings applied, {skipped} skipped on this plan).";
    }

    public static string RevertDisablePowerSaving(ProcessRunner runner)
    {
        runner.RunCommand("powercfg", "/setactive", "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
        return "High Performance plan re-applied (reset power saving overrides).";
    }
}
