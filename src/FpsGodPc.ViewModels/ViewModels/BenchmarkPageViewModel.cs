using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FpsGodPc.Core.Localization;
using FpsGodPc.Core.Services;
using FpsGodPc.Services;
using System.Collections.ObjectModel;

namespace FpsGodPc.App.ViewModels;

public partial class BenchmarkPageViewModel(AppServices services, LocalizationService l10n) : PageViewModelBase(services, l10n, "benchmark")
{
    public ObservableCollection<BenchmarkSession> Sessions { get; } = [];

    [ObservableProperty]
    private string label = string.Empty;

    [ObservableProperty]
    private int durationSeconds = 30;

    [ObservableProperty]
    private bool isRunning;

    [ObservableProperty]
    private string presentMonStatus = string.Empty;

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

    protected override void ApplyPageStrings()
    {
        base.ApplyPageStrings();
        LabelFieldLabel = L10n.BenchmarkLabelField;
        DurationLabel = L10n.BenchmarkDuration;
        RunBenchmarkLabel = L10n.BenchmarkRun;
        GridLabelHeader = L10n.GridLabel;
        GridTakenHeader = L10n.GridTaken;
        GridDurationHeader = L10n.GridDuration;
        GridFpsHeader = L10n.GridFps;
        GridPct1LowHeader = L10n.GridPct1Low;
        GridCpuPctHeader = L10n.GridCpuPct;
        if (string.IsNullOrWhiteSpace(Label) || Label is "Before tweaks" or "ก่อนปรับแต่ง")
        {
            Label = L10n.BenchmarkDefaultLabel;
        }

        UpdatePresentMonStatus();
        UpdateCompareSummary();
    }

    [RelayCommand]
    public void Refresh()
    {
        UpdatePresentMonStatus();
        UpdateCompareSummary();
        Sessions.Clear();
        foreach (var benchmark in Services.ListBenchmarks())
        {
            Sessions.Add(benchmark);
        }
    }

    [RelayCommand]
    public async Task RunAsync()
    {
        if (IsRunning)
        {
            return;
        }

        IsRunning = true;
        try
        {
            var duration = (uint)Math.Clamp(DurationSeconds, 10, 120);
            var result = await Task.Run(() => Services.RunBenchmark(Label, duration));
            StatusMessage = result.Message;
            Refresh();
        }
        finally
        {
            IsRunning = false;
        }
    }

    private void UpdatePresentMonStatus()
    {
        var pm = Services.GetPresentMonStatus();
        PresentMonStatus = L10n.BenchmarkPresentMonStatus(pm.Message);
    }

    private void UpdateCompareSummary()
    {
        var compare = Services.CompareLatestBenchmarks();
        if (compare is null)
        {
            CompareSummary = L10n.BenchmarkCompareNone;
            return;
        }

        if (compare.AfterFps is float after && compare.BeforeFps is float before)
        {
            var delta = compare.FpsDelta is float d ? $"{(d >= 0 ? "+" : "")}{d:F1}" : "—";
            CompareSummary = L10n.BenchmarkCompareFps($"{before:F1}", $"{after:F1}", delta);
            return;
        }

        CompareSummary = $"{compare.BeforeLabel} → {compare.AfterLabel}";
    }
}
