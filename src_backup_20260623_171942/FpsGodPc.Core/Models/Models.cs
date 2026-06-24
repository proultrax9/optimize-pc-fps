namespace FpsGodPc.Core.Models;

public enum RiskTier { Safe, Moderate, Advanced }

public sealed class CommandResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public static CommandResult Ok(string message) => new() { Success = true, Message = message };
    public static CommandResult Err(string message) => new() { Success = false, Message = message };
}

public sealed class TweakDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public RiskTier Tier { get; init; }
    public bool RequiresAdmin { get; init; }
    public bool RequiresReboot { get; init; }
    public bool AdvisorOnly { get; init; }
    public bool Reversible { get; init; } = true;
}

public sealed class TweakState
{
    public string Id { get; init; } = string.Empty;
    public bool Applied { get; init; }
    public string? AppliedAt { get; init; }
}

public sealed class FailedTweak
{
    public string Id { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
}

public sealed class ApplyBoostResult
{
    public List<string> Applied { get; set; } = [];
    public List<FailedTweak> Failed { get; set; } = [];
    public string Message { get; set; } = string.Empty;
}

public sealed class SpecGateResult
{
    public bool Allowed { get; init; }
    public string Message { get; init; } = string.Empty;
    public float? CpuTempC { get; init; }
    public float? GpuTempC { get; init; }
    public bool RequiresConfirmTimer { get; init; }
}

public sealed class ScanFinding
{
    public string Id { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
    public string Recommendation { get; init; } = string.Empty;
}

public sealed class ScanResult
{
    public List<ScanFinding> Findings { get; init; } = [];
    public string FpsGain { get; init; } = string.Empty;
    public string LatencyGain { get; init; } = string.Empty;
    public string StabilityRisk { get; init; } = string.Empty;
    public string RecommendedMode { get; init; } = string.Empty;
    public string? RecommendedPresetId { get; init; }
    public uint PerformanceScore { get; init; }
}

public sealed class CleanOptions
{
    public bool TempFiles { get; set; }
    public bool ShaderCache { get; set; }
    public bool DnsCache { get; set; }
    public bool RecycleBin { get; set; }
}

public sealed class CleanResult
{
    public double FreedMb { get; init; }
    public List<string> Items { get; init; } = [];
    public string Message { get; init; } = string.Empty;
}

public sealed class PingResult
{
    public string Host { get; init; } = string.Empty;
    public double? LatencyMs { get; init; }
    public double? PacketLoss { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed class GameProfile
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Executable { get; init; } = string.Empty;
    public uint FpsCap { get; init; }
    public string Priority { get; init; } = "Normal";
    public string LaunchOptions { get; init; } = string.Empty;
    public List<string> Notes { get; init; } = [];
    public bool Installed { get; init; }
    public string? InstallPath { get; init; }
}

public sealed class PresetBundle
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string RiskLevel { get; init; } = "safe";
    public string RiskLabel { get; init; } = "SAFE";
    public string Warning { get; init; } = string.Empty;
    public List<string> TweakIds { get; init; } = [];
    public List<string> TweakNames { get; init; } = [];
    public int MoreTweakCount { get; init; }
    public string IncludesLabel { get; init; } = string.Empty;
    public bool IsAdvisorOnly { get; init; }
    public bool RequiresRestorePoint { get; init; }
    public string? ApplyButtonLabel { get; init; }
}

public sealed class ExpertGuideStep
{
    public string Text { get; init; } = string.Empty;
}

public sealed class ExpertGuide
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Risk { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string? Warning { get; init; }
    public List<ExpertGuideStep> Steps { get; init; } = [];
}

public sealed class RollbackEntry
{
    public string TweakId { get; init; } = string.Empty;
    public string AppliedAt { get; init; } = string.Empty;
}

public sealed class RollbackInfo
{
    public List<RollbackEntry> Entries { get; init; } = [];
    public string? LastBoost { get; init; }
    public string? LastBoostAt { get; init; }
}

public sealed class RestorePoint
{
    public uint SequenceNumber { get; init; }
    public string Description { get; init; } = string.Empty;
    public string CreationTime { get; init; } = string.Empty;
}

public sealed class BenchmarkCompareResult
{
    public string BeforeLabel { get; init; } = string.Empty;
    public string AfterLabel { get; init; } = string.Empty;
    public float? BeforeFps { get; init; }
    public float? AfterFps { get; init; }
    public float? BeforePct1Low { get; init; }
    public float? AfterPct1Low { get; init; }
    public float? FpsDelta { get; init; }
    public float? Pct1LowDelta { get; init; }
}
