using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FpsGodPc.Core.Localization;
using FpsGodPc.Core.Models;
using FpsGodPc.Core.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace FpsGodPc.App.ViewModels;

public partial class BoostPageViewModel : PageViewModelBase
{
    private readonly Func<string?>? _openExpertChecklist;
    private readonly Action<string>? _navigate;

    public BoostPageViewModel(
        AppServices services,
        LocalizationService l10n,
        Func<string?>? openExpertChecklist = null,
        Action<string>? navigate = null)
        : base(services, l10n, "boost")
    {
        _openExpertChecklist = openExpertChecklist;
        _navigate = navigate;
    }

    public ObservableCollection<PresetBundle> Presets { get; } = [];

    [ObservableProperty]
    private bool isElevated;

    [ObservableProperty]
    private string restartAsAdminLabel = string.Empty;

    [ObservableProperty]
    private string openTweaksLabel = string.Empty;

    [ObservableProperty]
    private string moreTweaksLabel = string.Empty;

    [ObservableProperty]
    private string restoreRecommendedLabel = string.Empty;

    [RelayCommand]
    public void Refresh()
    {
        IsElevated = Services.IsElevated();
        Presets.Clear();
        foreach (var preset in Services.GetBoostPresets())
        {
            Presets.Add(preset);
        }
    }

    protected override void ApplyPageStrings()
    {
        base.ApplyPageStrings();
        RestartAsAdminLabel = L10n.RestartAsAdmin();
        OpenTweaksLabel = L10n.BoostOpenTweaks;
        MoreTweaksLabel = L10n.BoostMoreTweaks;
        RestoreRecommendedLabel = L10n.BoostRestoreRecommended;
        Refresh();
    }

    [RelayCommand]
    private void OpenTweaks() => _navigate?.Invoke("tweaks");

    [RelayCommand]
    public void RestartAsAdmin()
    {
        var msg = L10n.T("Restart the app as Administrator now?", "เปิดแอปใหม่แบบ Administrator ตอนนี้ไหม?");
        var title = L10n.T("Admin Required", "ต้องใช้สิทธิ์แอดมิน");
        if (!Confirm(msg, title))
        {
            return;
        }

        try
        {
            Services.RestartElevated();
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, L10n.T("Elevation Failed", "ยกระดับสิทธิ์ไม่สำเร็จ"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public async Task Apply(string? presetId)
    {
        if (string.IsNullOrWhiteSpace(presetId))
        {
            return;
        }

        var preset = Presets.FirstOrDefault(p => p.Id == presetId);
        if (preset is null)
        {
            return;
        }

        if (preset.IsAdvisorOnly)
        {
            OpenExpertChecklist();
            return;
        }

        if (!IsElevated)
        {
            var adminMsg = $"{L10n.T("Administrator rights are required for this boost.", "ชุดบูสต์นี้ต้องใช้สิทธิ์ Administrator")}\n\n{L10n.T("Restart as Administrator now?", "เปิดแอปใหม่แบบ Administrator ตอนนี้ไหม?")}";
            if (Confirm(adminMsg, L10n.T("Admin Required", "ต้องใช้สิทธิ์แอดมิน")))
            {
                RestartAsAdmin();
            }

            return;
        }

        var settings = Services.GetAppSettings();

        if (settings.ConfirmExtremeTweaks && presetId == "extreme")
        {
            var warn = $"{L10n.BoostExtremeWarning(preset.Name)}\n\n{preset.Description}";
            if (!Confirm(warn, L10n.T("Extreme Boost", "บูสต์สูงสุด")))
            {
                return;
            }
        }

        if (settings.CreateRestoreBeforeBoost && presetId is "competitive" or "extreme")
        {
            var rpMsg = L10n.T("Create a restore point before applying this boost?", "สร้างจุดคืนค่าก่อนใช้ Boost นี้ไหม?");
            if (Confirm(rpMsg))
            {
                await Task.Run(() => Services.CreateRestorePoint($"Before {preset.Name}"));
            }
        }

        var applyMsg = L10n.T($"Apply {preset.Name} preset?\n\n{preset.Description}", $"ใช้ {preset.Name}?\n\n{preset.Description}");
        var applyTitle = L10n.T("Apply Boost Preset", "ใช้ชุดบูสต์");
        if (!Confirm(applyMsg, applyTitle))
        {
            return;
        }

        var result = await Task.Run(() => Services.ApplyBoostPreset(presetId));
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

    private void OpenExpertChecklist()
    {
        if (_openExpertChecklist is null)
        {
            StatusMessage = L10n.AdvisorOnlyHint;
            return;
        }

        StatusMessage = _openExpertChecklist() ?? StatusMessage;
    }
}
