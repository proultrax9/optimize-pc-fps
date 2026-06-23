using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FpsGodPc.Core.Localization;
using FpsGodPc.Core.Models;
using FpsGodPc.Core.Services;
using FpsGodPc.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace FpsGodPc.App.ViewModels;

public partial class SafetyPageViewModel(AppServices services, LocalizationService l10n) : PageViewModelBase(services, l10n, "safety")
{
    public ObservableCollection<SnapshotSummary> Snapshots { get; } = [];
    public ObservableCollection<GuardianTweakItemViewModel> GuardianTweaks { get; } = [];

    [ObservableProperty]
    private int appliedCount;

    [ObservableProperty]
    private long? knownGoodId;

    [ObservableProperty]
    private bool hasPendingRevert;

    [ObservableProperty]
    private string? pendingRevertMessage;

    [ObservableProperty]
    private string createSnapshotLabel = string.Empty;

    [ObservableProperty]
    private string rollbackAllLabel = string.Empty;

    [ObservableProperty]
    private string dismissWatchdogLabel = string.Empty;

    [ObservableProperty]
    private string guardianTweaksLabel = string.Empty;

    [ObservableProperty]
    private string snapshotsSectionLabel = string.Empty;

    [ObservableProperty]
    private string gridIdHeader = string.Empty;

    [ObservableProperty]
    private string gridLabelHeader = string.Empty;

    [ObservableProperty]
    private string gridTakenHeader = string.Empty;

    [ObservableProperty]
    private string gridKnownGoodHeader = string.Empty;

    [ObservableProperty]
    private string appliedSummary = string.Empty;

    [ObservableProperty]
    private string knownGoodSummary = string.Empty;

    [ObservableProperty]
    private string pendingRevertKeepLabel = string.Empty;

    [ObservableProperty]
    private string pendingRevertRevertLabel = string.Empty;

    protected override void ApplyPageStrings()
    {
        base.ApplyPageStrings();
        CreateSnapshotLabel = L10n.CreateSnapshot;
        RollbackAllLabel = L10n.RollbackAll;
        DismissWatchdogLabel = L10n.DismissWatchdog;
        GuardianTweaksLabel = L10n.SafetyGuardianTweaks;
        SnapshotsSectionLabel = L10n.SafetySnapshotsSection;
        GridIdHeader = L10n.GridId;
        GridLabelHeader = L10n.GridLabel;
        GridTakenHeader = L10n.GridTaken;
        GridKnownGoodHeader = L10n.GridKnownGood;
        PendingRevertKeepLabel = L10n.PendingRevertKeep;
        PendingRevertRevertLabel = L10n.PendingRevertRevert;
        UpdateSummaries();
        RefreshGuardianLabels();
    }

    [RelayCommand]
    public void Refresh()
    {
        var safety = Services.GetSafetyStatus();
        AppliedCount = safety.AppliedTweakCount;
        KnownGoodId = safety.LastKnownGoodId;
        HasPendingRevert = safety.PendingRevert;
        PendingRevertMessage = safety.PendingRevert && !string.IsNullOrWhiteSpace(safety.PendingReason)
            ? L10n.PendingRevertBanner(safety.PendingReason!)
            : null;
        UpdateSummaries();

        Snapshots.Clear();
        foreach (var snapshot in Services.ListSnapshots())
        {
            Snapshots.Add(snapshot);
        }

        RefreshGuardianTweaks();
    }

    [RelayCommand]
    public void CreateSnapshot()
    {
        var label = L10n.T("Manual safety snapshot", "Snapshot ความปลอดภัยด้วยตนเอง");
        StatusMessage = Services.CreateSnapshot(label).Message;
        Refresh();
    }

    [RelayCommand]
    public void RollbackAll()
    {
        var msg = L10n.T("Rollback all tracked tweaks now?", "ย้อนทวีคที่ติดตามทั้งหมดตอนนี้ไหม?");
        var title = L10n.T("Rollback All", "ย้อนทั้งหมด");
        if (!Confirm(msg, title))
        {
            return;
        }

        StatusMessage = Services.RollbackAll().Message;
        Refresh();
    }

    [RelayCommand]
    public void DismissWatchdog() => StatusMessage = Services.DismissCrashWatchdog().Message;

    [RelayCommand]
    public void RestoreSnapshot(long snapshotId)
    {
        var msg = L10n.T($"Restore snapshot #{snapshotId}?", $"คืนค่า snapshot #{snapshotId}?");
        var title = L10n.T("Restore Snapshot", "คืนค่า Snapshot");
        if (!Confirm(msg, title))
        {
            return;
        }

        StatusMessage = Services.RestoreSnapshot(snapshotId).Message;
        Refresh();
    }

    [RelayCommand]
    public void KeepPendingTweaks()
    {
        StatusMessage = Services.ConfirmPendingRevert().Message;
        Refresh();
    }

    [RelayCommand]
    public void RejectPendingTweaks()
    {
        StatusMessage = Services.RejectPendingRevert().Message;
        Refresh();
    }

    private void RefreshGuardianTweaks()
    {
        GuardianTweaks.Clear();
        foreach (var tweak in Services.GetGuardianTweaks())
        {
            GuardianTweaks.Add(new GuardianTweakItemViewModel(
                tweak.Id,
                L10n.GuardianTweakName(tweak.Id),
                Services.IsGuardianTweakApplied(tweak.Id),
                L10n.Risk(tweak.Tier),
                ToggleGuardian));
        }
    }

    private void RefreshGuardianLabels()
    {
        foreach (var item in GuardianTweaks)
        {
            item.UpdateLabel(L10n.GuardianTweakName(item.Id));
        }
    }

    private void ToggleGuardian(GuardianTweakItemViewModel item, bool enabled)
    {
        if (!enabled)
        {
            var off = Services.SetGuardianTweakState(item.Id, false);
            StatusMessage = off.Message;
            if (!off.Success)
            {
                item.RevertToggle();
            }

            Refresh();
            return;
        }

        var gate = Services.SpecGate(item.Id);
        if (!gate.Allowed)
        {
            MessageBox.Show(gate.Message, L10n.ConfirmTitle(), MessageBoxButton.OK, MessageBoxImage.Warning);
            item.RevertToggle();
            return;
        }

        if (gate.RequiresConfirmTimer)
        {
            var msg = $"{gate.Message}\n\n{L10n.T("Continue applying this tweak?", "ใช้ทวีคนี้ต่อไหม?")}";
            if (!Confirm(msg))
            {
                item.RevertToggle();
                return;
            }
        }

        var result = Services.SetGuardianTweakState(item.Id, true);
        StatusMessage = result.Message;
        if (!result.Success)
        {
            item.RevertToggle();
        }

        Refresh();
    }

    private void UpdateSummaries()
    {
        AppliedSummary = L10n.T($"Applied guardian tweaks: {AppliedCount}", $"ทวีค Guardian ที่ใช้: {AppliedCount}");
        KnownGoodSummary = KnownGoodId is null
            ? L10n.T("Last known good snapshot: —", "Snapshot ที่ดีล่าสุด: —")
            : L10n.T($"Last known good snapshot: {KnownGoodId}", $"Snapshot ที่ดีล่าสุด: {KnownGoodId}");
    }
}

public partial class GuardianTweakItemViewModel(string id, string name, bool enabled, string riskLabel, Action<GuardianTweakItemViewModel, bool> onToggle) : ViewModelBase
{
    public string Id { get; } = id;

    [ObservableProperty]
    private string name = name;

    public string RiskLabel { get; } = riskLabel;

    [ObservableProperty]
    private bool isEnabled = enabled;

    public void UpdateLabel(string label) => Name = label;

    [RelayCommand]
    private void Toggle()
    {
        var next = !IsEnabled;
        IsEnabled = next;
        onToggle(this, next);
    }

    public void RevertToggle() => IsEnabled = !IsEnabled;
}
