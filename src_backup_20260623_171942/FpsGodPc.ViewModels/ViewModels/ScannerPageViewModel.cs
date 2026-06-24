using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FpsGodPc.Core.Localization;
using FpsGodPc.Core.Services;
using FpsGodPc.Core.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace FpsGodPc.App.ViewModels;

public partial class ScannerPageViewModel(AppServices services, LocalizationService l10n) : PageViewModelBase(services, l10n, "scanner")
{
    private bool _isScanning;

    public ObservableCollection<ScanFinding> Findings { get; } = [];

    [ObservableProperty]
    private string fpsGain = "—";

    [ObservableProperty]
    private string latencyGain = "—";

    [ObservableProperty]
    private string stabilityRisk = "—";

    [ObservableProperty]
    private string recommendedMode = "—";

    [ObservableProperty]
    private uint performanceScore;

    [ObservableProperty]
    private string? recommendedPresetId;

    [ObservableProperty]
    private string runScanLabel = string.Empty;

    [ObservableProperty]
    private string applyRecommendedLabel = string.Empty;

    [ObservableProperty]
    private string fpsGainLabel = string.Empty;

    [ObservableProperty]
    private string latencyGainLabel = string.Empty;

    [ObservableProperty]
    private string stabilityRiskLabel = string.Empty;

    [ObservableProperty]
    private string recommendedLabel = string.Empty;

    protected override void ApplyPageStrings()
    {
        base.ApplyPageStrings();
        RunScanLabel = L10n.ScannerRunScan;
        ApplyRecommendedLabel = L10n.ScannerApplyRecommended;
        FpsGainLabel = L10n.ScannerFpsGain;
        LatencyGainLabel = L10n.ScannerLatencyGain;
        StabilityRiskLabel = L10n.ScannerStabilityRisk;
        RecommendedLabel = L10n.ScannerRecommended;

        if (Findings.Count > 0)
        {
            _ = RunScan();
        }
    }

    [RelayCommand]
    public async Task RunScan()
    {
        if (_isScanning)
        {
            return;
        }

        _isScanning = true;
        try
        {
            var result = await Task.Run(() => Services.RunScanner());

            await UiDispatch.InvokeAsync(() =>
            {
                FpsGain = result.FpsGain;
                LatencyGain = result.LatencyGain;
                StabilityRisk = result.StabilityRisk;
                RecommendedMode = result.RecommendedMode;
                RecommendedPresetId = result.RecommendedPresetId;
                PerformanceScore = result.PerformanceScore;

                Findings.Clear();
                foreach (var finding in result.Findings)
                {
                    Findings.Add(finding);
                }
            });
        }
        finally
        {
            _isScanning = false;
        }
    }

    [RelayCommand]
    public async Task ApplyRecommended()
    {
        if (string.IsNullOrWhiteSpace(RecommendedPresetId))
        {
            return;
        }

        if (RecommendedPresetId == "extreme")
        {
            var warn = L10n.BoostExtremeWarning(L10n.BoostName("extreme"));
            if (!Confirm(warn, L10n.BoostName("extreme")))
            {
                return;
            }
        }

        var applyMsg = L10n.T(
            $"Apply {RecommendedMode} preset from scanner recommendation?",
            $"ใช้ {RecommendedMode} ตามที่สแกนเนอร์แนะนำไหม?");
        if (!Confirm(applyMsg))
        {
            return;
        }

        var recommendedPresetId = RecommendedPresetId;
        var result = await Task.Run(() => Services.ApplyBoostPreset(recommendedPresetId));
        StatusMessage = result.Message;
        if (result.Failed.Count > 0)
        {
            MessageBox.Show(
                string.Join(Environment.NewLine, result.Failed.Select(f => $"{L10n.TweakName(f.Id)}: {L10n.LocalizeResult(f.Error)}")),
                L10n.T("Some Tweaks Failed", "บางทวีคล้มเหลว"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
