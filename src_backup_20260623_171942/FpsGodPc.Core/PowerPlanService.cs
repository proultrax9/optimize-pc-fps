using System.Text.RegularExpressions;
using FpsGodPc.Core.Models;
using FpsGodPc.Services;

namespace FpsGodPc.Core;

public sealed class PowerPlanService
{
    private const string HighPerformanceGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
    private const string UltimatePerformanceGuid = "e9a42b02-d5df-448d-aa00-03f14749eb61";
    private const string GodModePlanName = "God Mode";

    private static readonly string[] DuplicateSourceGuids =
    [
        UltimatePerformanceGuid,
        HighPerformanceGuid,
        "381b4222-f694-41f0-9685-ff5bb260df2e",
    ];

    private static readonly (string Subgroup, string Setting, string Value)[] GodModeSettings =
    [
        ("SUB_VIDEO", "VIDEOIDLE", "0"),
        ("SUB_SLEEP", "STANDBYIDLE", "0"),
        ("SUB_SLEEP", "HIBERNATEIDLE", "0"),
        ("SUB_PROCESSOR", "PROCTHROTTLEMIN", "100"),
        ("SUB_PROCESSOR", "PROCTHROTTLEMAX", "100"),
        ("SUB_PCIEXPRESS", "ASPM", "0"),
        ("SUB_DISK", "DISKIDLE", "0"),
    ];

    private readonly ProcessRunner _runner;

    public PowerPlanService(ProcessRunner runner) => _runner = runner;

    public CommandResult ApplyGodModePlan()
    {
        try
        {
            var planGuid = EnsureGodModePlan();
            var applied = 0;

            foreach (var (subgroup, setting, value) in GodModeSettings)
            {
                if (TrySetPlanValue(planGuid, subgroup, setting, value))
                {
                    applied++;
                }
            }

            _runner.RunCommand("powercfg", "/setactive", planGuid);

            TryRun("powercfg", "/change", "monitor-timeout-ac", "0");
            TryRun("powercfg", "/change", "monitor-timeout-dc", "0");
            TryRun("powercfg", "/change", "standby-timeout-ac", "0");
            TryRun("powercfg", "/change", "standby-timeout-dc", "0");
            TryRun("powercfg", "/change", "hibernate-timeout-ac", "0");
            TryRun("powercfg", "/change", "hibernate-timeout-dc", "0");
            TryRun("powercfg", "/change", "disk-timeout-ac", "0");
            TryRun("powercfg", "/change", "disk-timeout-dc", "0");

            return applied > 0
                ? CommandResult.Ok("God Mode power plan is active. Display and sleep are disabled; CPU runs at maximum.")
                : CommandResult.Ok("God Mode power plan activated with default High Performance settings.");
        }
        catch (Exception ex)
        {
            return CommandResult.Err($"Unable to apply God Mode power plan: {ex.Message}");
        }
    }

    private string EnsureGodModePlan()
    {
        var existingGuid = FindGodModePlanGuid();
        if (!string.IsNullOrWhiteSpace(existingGuid))
        {
            return existingGuid;
        }

        foreach (var sourceGuid in DuplicateSourceGuids)
        {
            if (!PlanExists(sourceGuid))
            {
                continue;
            }

            try
            {
                var duplicateOutput = _runner.RunCommand("powercfg", "/duplicatescheme", sourceGuid);
                var newGuid = ExtractFirstGuid(duplicateOutput);
                if (string.IsNullOrWhiteSpace(newGuid))
                {
                    continue;
                }

                _runner.RunCommand(
                    "powercfg",
                    "/changename",
                    newGuid,
                    GodModePlanName,
                    "Maximum performance — display stays on, no sleep.");

                return newGuid;
            }
            catch
            {
                // Try the next source plan.
            }
        }

        throw new InvalidOperationException("Could not create God Mode plan. Ensure High Performance or Ultimate Performance exists.");
    }

    private bool PlanExists(string guid)
    {
        try
        {
            var plansOutput = _runner.RunCommand("powercfg", "/list");
            return plansOutput.Contains(guid, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private bool TrySetPlanValue(string planGuid, string subgroup, string setting, string value)
    {
        if (!TryRun("powercfg", "-setacvalueindex", planGuid, subgroup, setting, value))
        {
            return false;
        }

        TryRun("powercfg", "-setdcvalueindex", planGuid, subgroup, setting, value);
        return true;
    }

    private bool TryRun(string program, params string[] args)
    {
        try
        {
            _runner.RunCommand(program, args);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private string? FindGodModePlanGuid()
    {
        var plansOutput = _runner.RunCommand("powercfg", "/list");
        foreach (var line in plansOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!line.Contains(GodModePlanName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var guid = ExtractFirstGuid(line);
            if (!string.IsNullOrWhiteSpace(guid))
            {
                return guid;
            }
        }

        return null;
    }

    private static string? ExtractFirstGuid(string text)
    {
        var match = Regex.Match(text, @"\b[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}\b");
        return match.Success ? match.Value : null;
    }
}
