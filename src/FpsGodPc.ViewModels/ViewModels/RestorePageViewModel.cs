using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FpsGodPc.Core.Localization;
using FpsGodPc.Core.Services;
using FpsGodPc.Core.Models;
using System.Collections.ObjectModel;

namespace FpsGodPc.App.ViewModels;

public partial class RestorePageViewModel(AppServices services, LocalizationService l10n) : PageViewModelBase(services, l10n, "restore")
{
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
    public void Refresh()
    {
        var rollback = Services.GetRollbackInfo();
        LastBoost = L10n.LocalizeBoostDisplayName(rollback.LastBoost);
        LastBoostAt = rollback.LastBoostAt;

        AppliedEntries.Clear();
        foreach (var entry in rollback.Entries)
        {
            AppliedEntries.Add(new AppliedTweakRow(
                entry.TweakId,
                L10n.TweakName(entry.TweakId),
                entry.AppliedAt));
        }

        RestorePoints.Clear();
        foreach (var point in Services.ListRestorePoints())
        {
            RestorePoints.Add(point);
        }
    }

    [RelayCommand]
    public void CreateRestorePointAction()
    {
        StatusMessage = Services.CreateRestorePoint(L10n.RestoreManualPointDescription).Message;
        Refresh();
    }

    [RelayCommand]
    public void RollbackAll()
    {
        if (!Confirm(L10n.RestoreRollbackConfirm, L10n.RestoreRollbackAll))
        {
            return;
        }

        StatusMessage = Services.RollbackAll().Message;
        Refresh();
    }
}
