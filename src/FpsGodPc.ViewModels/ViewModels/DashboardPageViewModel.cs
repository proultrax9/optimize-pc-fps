using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FpsGodPc.Core.Localization;
using FpsGodPc.Core.Services;
using FpsGodPc.Services;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace FpsGodPc.App.ViewModels;

public partial class DashboardPageViewModel(AppServices services, LocalizationService l10n) : PageViewModelBase(services, l10n, "dashboard")
{
    private bool _isRefreshing;
    private DispatcherTimer? _sampler;
    private bool _sampling;

    public ObservableCollection<MetricItem> LiveMetrics { get; } = [];
    public ObservableCollection<MetricItem> SystemMetrics { get; } = [];

    // Numeric live values for the HyperTune-style radial gauges + live graph.
    public ObservableCollection<double> CpuHistory { get; } = [];

    [ObservableProperty]
    private double cpuUsage;

    [ObservableProperty]
    private double gpuUsage;

    [ObservableProperty]
    private double ramUsage;

    [ObservableProperty]
    private double cpuTemp;

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
    public async Task Refresh()
    {
        if (_isRefreshing)
        {
            return;
        }

        _isRefreshing = true;
        try
        {
            var snapshot = await Task.Run(() =>
            {
                var info = Services.CollectSystemInfo();
                var telemetry = Services.CollectTelemetry();
                var safety = Services.GetSafetyStatus();
                var lastBenchmark = Services.ListBenchmarks().FirstOrDefault(b => b.AvgFps is > 0f);
                return (info, telemetry, safety, lastBenchmark);
            });

            await UiDispatch.InvokeAsync(() =>
            {
                var info = snapshot.info;
                var telemetry = snapshot.telemetry;
                var safety = snapshot.safety;
                var lastBenchmark = snapshot.lastBenchmark;

                PerformanceScore = info.PerformanceScore.ToString();
                // The score is the hardware-capability tier (real CPU/GPU/RAM of this PC).
                // The detail line surfaces the last REAL benchmark we ran, so the user sees
                // both "power of this PC" and the FPS actually measured by the benchmark.
                PerformanceScoreDetail = lastBenchmark?.AvgFps is float fps
                    ? $"Hardware tier {info.PerformanceScore}/100 · last benchmark {fps:F0} FPS ({lastBenchmark.Source})"
                    : $"Hardware tier {info.PerformanceScore}/100 · run a benchmark to measure real FPS";
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

                ApplyTelemetry(telemetry);
            });

            EnsureSampler();
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private void ApplyTelemetry(TelemetrySnapshot telemetry)
    {
        CpuUsage = telemetry.CpuUsagePct;
        GpuUsage = telemetry.GpuUsagePct ?? 0d;
        CpuTemp = telemetry.CpuTempC ?? 0d;
        double total = telemetry.MemoryTotalGb;
        RamUsage = total > 0 ? telemetry.MemoryUsedGb / total * 100d : 0d;

        CpuHistory.Add(CpuUsage);
        while (CpuHistory.Count > 40)
        {
            CpuHistory.RemoveAt(0);
        }
    }

    // Live-samples telemetry every 2s while the dashboard is shown so the
    // gauges and graph animate. Stopped on Dispose (navigation away).
    private void EnsureSampler()
    {
        if (_sampler is not null)
        {
            return;
        }

        _sampler = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _sampler.Tick += async (_, _) => await SampleLive();
        _sampler.Start();
    }

    private async Task SampleLive()
    {
        if (_sampling)
        {
            return;
        }

        _sampling = true;
        try
        {
            var telemetry = await Task.Run(() => Services.CollectTelemetry());
            await UiDispatch.InvokeAsync(() => ApplyTelemetry(telemetry));
        }
        finally
        {
            _sampling = false;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _sampler?.Stop();
            _sampler = null;
        }

        base.Dispose(disposing);
    }
}
