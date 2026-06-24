using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FpsGodPc.Core.Localization;
using FpsGodPc.Core.Models;
using FpsGodPc.Core.Services;
using FpsGodPc.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace FpsGodPc.App.ViewModels;

public partial class BenchmarkPageViewModel(AppServices services, LocalizationService l10n) : PageViewModelBase(services, l10n, "benchmark")
{
    private bool _isRefreshing;

    public ObservableCollection<BenchmarkSession> Sessions { get; } = [];

    [ObservableProperty]
    private string label = string.Empty;

    [ObservableProperty]
    private int durationSeconds = 60;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunBenchmarkCommand))]
    private bool isRunning;

    [ObservableProperty]
    private string heavenStatus = "Checking Heaven benchmark...";

    [ObservableProperty]
    private bool isHeavenReady;

    [ObservableProperty]
    private string compareSummary = string.Empty;

    [ObservableProperty]
    private string labelFieldLabel = string.Empty;

    [ObservableProperty]
    private string durationLabel = string.Empty;

    [ObservableProperty]
    private string runBenchmarkLabel = string.Empty;

    [ObservableProperty]
    private string gridLabelHeader = string.Empty;

    [ObservableProperty]
    private string gridTakenHeader = string.Empty;

    [ObservableProperty]
    private string gridDurationHeader = string.Empty;

    [ObservableProperty]
    private string gridFpsHeader = string.Empty;

    [ObservableProperty]
    private string gridPct1LowHeader = string.Empty;

    [ObservableProperty]
    private string gridCpuPctHeader = string.Empty;

    [ObservableProperty]
    private string gridSourceHeader = string.Empty;

    protected override void ApplyPageStrings()
    {
        base.ApplyPageStrings();
        LabelFieldLabel = L10n.BenchmarkLabelField;
        DurationLabel = L10n.BenchmarkDuration;
        RunBenchmarkLabel = L10n.BenchmarkRun3D;
        GridLabelHeader = L10n.GridLabel;
        GridTakenHeader = L10n.GridTaken;
        GridDurationHeader = L10n.GridDuration;
        GridFpsHeader = L10n.GridFps;
        GridPct1LowHeader = L10n.GridPct1Low;
        GridCpuPctHeader = L10n.GridCpuPct;
        GridSourceHeader = "Source";
        if (string.IsNullOrWhiteSpace(Label) || Label is "Before tweaks" or "ก่อนปรับแต่ง")
        {
            Label = L10n.BenchmarkDefaultLabel;
        }

        CompareSummary = L10n.BenchmarkCompareNone;
        RunBenchmarkCommand.NotifyCanExecuteChanged();
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
            var data = await Task.Run(() =>
            {
                var heaven = Services.GetGpuBenchmarkStatus();
                var compare = Services.CompareLatestBenchmarks();
                var sessions = Services.ListBenchmarks();
                return (heaven, compare, sessions);
            });

            await UiDispatch.InvokeAsync(() =>
            {
                ApplyHeavenStatus(data.heaven);
                ApplyCompareSummary(data.compare, data.sessions);
                Sessions.Clear();
                foreach (var benchmark in data.sessions)
                {
                    Sessions.Add(benchmark);
                }
            });
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRunBenchmark))]
    public async Task RunBenchmark()
    {
        if (IsRunning)
        {
            return;
        }

        IsRunning = true;
        try
        {
            var duration = (uint)Math.Clamp(DurationSeconds, 10, 600);
            var resolvedLabel = string.IsNullOrWhiteSpace(Label) ? L10n.BenchmarkDefaultLabel : Label;
            var progress = new Progress<string>(msg =>
                UiDispatch.InvokeAsync(() => StatusMessage = msg));

            if (!IsHeavenReady)
            {
                StatusMessage = L10n.BenchmarkDownloadingMessage;
                var ensure = await Services.EnsureGpuBenchmarkAsync(progress);
                await UiDispatch.InvokeAsync(() =>
                {
                    StatusMessage = ensure.Message;
                    ApplyHeavenStatus(Services.GetGpuBenchmarkStatus());
                });
                if (!ensure.Success)
                {
                    MessageBox.Show(ensure.Message, "FPS Optimize GOD PC", MessageBoxButton.OK, MessageBoxImage.Warning);
                    IsRunning = false;
                    return;
                }
            }
            else
            {
                _ = await Services.EnsurePresentMonAsync(progress);
            }

            var result = await Services.RunAutomatedHeavenBenchmarkAsync(
                resolvedLabel,
                duration,
                progress);

            StatusMessage = result.Message;
            if (!result.Success)
            {
                MessageBox.Show(result.Message, "FPS Optimize GOD PC", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            await Refresh();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            MessageBox.Show(ex.Message, "FPS Optimize GOD PC", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsRunning = false;
            RunBenchmarkCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanRunBenchmark() => !IsRunning;

    private void ApplyHeavenStatus(UnigineBenchmarkStatus status)
    {
        IsHeavenReady = status.Available;
        HeavenStatus = status.Message;
    }

    private void ApplyCompareSummary(BenchmarkCompareResult? compare, IReadOnlyList<BenchmarkSession>? sessions = null)
    {
        if (compare is not null && compare.AfterFps is float after && compare.BeforeFps is float before)
        {
            var delta = compare.FpsDelta is float d ? $"{(d >= 0 ? "+" : "")}{d:F1}" : "—";
            CompareSummary = L10n.BenchmarkCompareFps($"{before:F1}", $"{after:F1}", delta);
            return;
        }

        if (compare is not null)
        {
            CompareSummary = $"{compare.BeforeLabel} → {compare.AfterLabel}";
            return;
        }

        var latest = sessions?.FirstOrDefault(s => s.AvgFps is float);
        if (latest?.AvgFps is float fps)
        {
            CompareSummary = L10n.BenchmarkLatestResult(fps, latest.Source ?? "benchmark");
            return;
        }

        CompareSummary = L10n.BenchmarkCompareNone;
    }
}
