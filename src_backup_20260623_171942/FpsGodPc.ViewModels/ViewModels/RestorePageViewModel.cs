using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FpsGodPc.Core.Localization;
using FpsGodPc.Core.Services;
using FpsGodPc.Core.Models;
using System.Collections.ObjectModel;

namespace FpsGodPc.App.ViewModels;

public partial class RestorePageViewModel(AppServices services, LocalizationService l10n) : PageViewModelBase(services, l10n, "restore")
{
    private bool _isRefreshing;

    public ObservableCollection<AppliedTweakRow> AppliedEntries { get; } = [];
    public ObservableCollection<RestorePoint> RestorePoints { get; } = [];

    [ObservableProperty]
    private string? lastBoost;

    [ObservableProperty]
    private string? lastBoostAt;

    [ObservableProperty]
    private string createRestorePointLabel = string.Empty;

    [ObservableProperty]
    private string rollbackAllTweaksLabel = string.Empty;

    [ObservableProperty]
    private string restorePointsSectionLabel = string.Empty;

    [ObservableProperty]
    private string appliedTweaksSectionLabel = string.Empty;

    [ObservableProperty]
    private string lastBoostPrefix = string.Empty;

    [ObservableProperty]
    private string gridSequenceHeader = string.Empty;

    [ObservableProperty]
    private string gridDescriptionHeader = string.Empty;

    [ObservableProperty]
    private string gridCreatedHeader = string.Empty;

    [ObservableProperty]
    private string gridTweakIdHeader = string.Empty;

    [ObservableProperty]
    private string gridAppliedAtHeader = string.Empty;

    protected override void ApplyPageStrings()
    {
        base.ApplyPageStrings();
        CreateRestorePointLabel = L10n.RestoreCreatePoint;
        RollbackAllTweaksLabel = L10n.RestoreRollbackAll;
        RestorePointsSectionLabel = L10n.RestorePointsSection;
        AppliedTweaksSectionLabel = L10n.RestoreAppliedSection;
        LastBoostPrefix = L10n.RestoreLastBoostPrefix;
        GridSequenceHeader = L10n.GridSequence;
        GridDescriptionHeader = L10n.GridDescription;
        GridCreatedHeader = L10n.GridCreated;
        GridTweakIdHeader = L10n.GridTweakId;
        GridAppliedAtHeader = L10n.GridAppliedAt;
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
                var rollback = Services.GetRollbackInfo();
                var points = Services.ListRestorePoints();
                return (rollback, points);
            });

            await UiDispatch.InvokeAsync(() =>
            {
                LastBoost = L10n.LocalizeBoostDisplayName(snapshot.rollback.LastBoost);
                LastBoostAt = snapshot.rollback.LastBoostAt;

                AppliedEntries.Clear();
                foreach (var entry in snapshot.rollback.Entries)
                {
                    AppliedEntries.Add(new AppliedTweakRow(
                        entry.TweakId,
                        L10n.TweakName(entry.TweakId),
                        entry.AppliedAt));
                }

                RestorePoints.Clear();
                foreach (var point in snapshot.points)
                {
                    RestorePoints.Add(point);
                }
            });
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    [RelayCommand]
    public async Task CreateRestorePointAction()
    {
        var result = await Task.Run(() => Services.CreateRestorePoint(L10n.RestoreManualPointDescription));
        StatusMessage = result.Message;
        await Refresh();
    }

    [RelayCommand]
    public async Task RollbackAll()
    {
        if (!Confirm(L10n.RestoreRollbackConfirm, L10n.RestoreRollbackAll))
        {
            return;
        }

        var result = await Task.Run(() => Services.RollbackAll());
        StatusMessage = result.Message;
        await Refresh();
    }
}
