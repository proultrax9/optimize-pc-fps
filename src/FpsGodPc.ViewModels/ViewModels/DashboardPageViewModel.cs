using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FpsGodPc.Core.Localization;
using FpsGodPc.Core.Services;
using System.Collections.ObjectModel;

namespace FpsGodPc.App.ViewModels;

public partial class DashboardPageViewModel(AppServices services, LocalizationService l10n) : PageViewModelBase(services, l10n, "dashboard")
{
    public ObservableCollection<MetricItem> LiveMetrics { get; } = [];
    public ObservableCollection<MetricItem> SystemMetrics { get; } = [];

    [ObservableProperty]
    private string performanceScore = "0";

    [ObservableProperty]
    private string safetyScore = "0";

    [ObservableProperty]
    private string powerPlan = "—";

    [ObservableProperty]
    private string gameMode = "Disabled";

    [ObservableProperty]
    private string performanceScoreLabel = string.Empty;

    [ObservableProperty]
    private string safetyScoreLabel = string.Empty;

    [ObservableProperty]
    private string powerPlanLabel = string.Empty;

    [ObservableProperty]
    private string gameModeLabel = string.Empty;

    [ObservableProperty]
    private string performanceScoreDetail = string.Empty;

    [ObservableProperty]
    private string safetyScoreDetail = string.Empty;

    [ObservableProperty]
    private string powerPlanDetail = string.Empty;

    [ObservableProperty]
    private string gameModeDetail = string.Empty;

    [ObservableProperty]
    private string liveHardwareLabel = string.Empty;

    [ObservableProperty]
    private string systemDetailsLabel = string.Empty;

    protected override void ApplyPageStrings()
    {
        base.ApplyPageStrings();
        PerformanceScoreLabel = L10n.DashboardPerformanceScore;
        SafetyScoreLabel = L10n.DashboardSafetyScore;
        PowerPlanLabel = L10n.DashboardPowerPlan;
        GameModeLabel = L10n.DashboardGameMode;
        PerformanceScoreDetail = L10n.DashboardPerformanceDetail;
        SafetyScoreDetail = L10n.DashboardSafetyDetail;
        PowerPlanDetail = L10n.DashboardPowerPlanDetail;
        GameModeDetail = L10n.DashboardGameModeDetail;
        LiveHardwareLabel = L10n.DashboardLiveHardware;
        SystemDetailsLabel = L10n.DashboardSystemDetails;
    }

    [RelayCommand]
    public void Refresh()
    {
        var info = Services.CollectSystemInfo();
        var telemetry = Services.CollectTelemetry();
        var safety = Services.GetSafetyStatus();

        PerformanceScore = info.PerformanceScore.ToString();
        SafetyScore = Math.Clamp(100 - safety.AppliedTweakCount * 4, 0, 100).ToString();
        PowerPlan = info.PowerPlan;
        GameMode = info.GameModeEnabled ? L10n.Enabled : L10n.Disabled;

        LiveMetrics.Clear();
        LiveMetrics.Add(new MetricItem(L10n.MetricCpu, $"{telemetry.CpuUsagePct:F0}%", telemetry.CpuName));
        LiveMetrics.Add(new MetricItem(L10n.MetricGpu, telemetry.GpuUsagePct is null ? "—" : $"{telemetry.GpuUsagePct:F0}%", telemetry.GpuName));
        LiveMetrics.Add(new MetricItem(L10n.MetricMemory, $"{telemetry.MemoryUsedGb:F1}/{telemetry.MemoryTotalGb:F1} GB", null));
        LiveMetrics.Add(new MetricItem(L10n.MetricProcesses, telemetry.ProcessCount.ToString(), null));

        SystemMetrics.Clear();
        SystemMetrics.Add(new MetricItem(L10n.MetricOperatingSystem, info.OsName, info.OsVersion));
        SystemMetrics.Add(new MetricItem(L10n.MetricStorage, info.StorageName, $"{info.StorageTotalGb} GB"));
        SystemMetrics.Add(new MetricItem(L10n.MetricCpu, info.CpuName, $"{info.CpuPhysicalCores}C / {info.CpuCores}T"));
        SystemMetrics.Add(new MetricItem(L10n.MetricGpu, info.GpuName, $"{info.GpuVramGb} GB VRAM"));
    }
}
