using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FpsGodPc.Core.Localization;
using FpsGodPc.Core.Services;
using FpsGodPc.Services;
using System.Collections.ObjectModel;

namespace FpsGodPc.App.ViewModels;

public partial class SettingsPageViewModel(AppServices services, LocalizationService l10n) : PageViewModelBase(services, l10n, "settings")
{
    public ObservableCollection<string> Languages { get; } = [ "en", "th" ];

    [ObservableProperty]
    private string selectedLanguage = "en";

    [ObservableProperty]
    private bool createRestoreBeforeBoost = true;

    [ObservableProperty]
    private bool confirmExtremeTweaks = true;

    [ObservableProperty]
    private bool watcherEnabled = true;

    [ObservableProperty]
    private bool bootAutoRevert = true;

    [ObservableProperty]
    private int confirmTimerSecs = 15;

    [ObservableProperty]
    private string dataDirectory = string.Empty;

    [ObservableProperty]
    private string guardianDirectory = string.Empty;

    [ObservableProperty]
    private string versionLabel = string.Empty;

    [ObservableProperty]
    private string appVersion = string.Empty;

    [ObservableProperty]
    private string saveSettingsLabel = string.Empty;

    [ObservableProperty]
    private string languageLabel = string.Empty;

    [ObservableProperty]
    private string createRestoreBeforeBoostLabel = string.Empty;

    [ObservableProperty]
    private string confirmExtremeTweaksLabel = string.Empty;

    [ObservableProperty]
    private string watcherEnabledLabel = string.Empty;

    [ObservableProperty]
    private string bootAutoRevertLabel = string.Empty;

    [ObservableProperty]
    private string confirmTimerLabel = string.Empty;

    [ObservableProperty]
    private string appDataPrefix = string.Empty;

    [ObservableProperty]
    private string guardianDataPrefix = string.Empty;

    protected override void ApplyPageStrings()
    {
        base.ApplyPageStrings();
        SaveSettingsLabel = L10n.SaveSettings;
        LanguageLabel = L10n.SettingsLanguage;
        CreateRestoreBeforeBoostLabel = L10n.SettingsCreateRestoreBeforeBoost;
        ConfirmExtremeTweaksLabel = L10n.SettingsConfirmExtremeTweaks;
        WatcherEnabledLabel = L10n.SettingsWatcherEnabled;
        BootAutoRevertLabel = L10n.SettingsBootAutoRevert;
        ConfirmTimerLabel = L10n.SettingsConfirmTimer;
        AppDataPrefix = L10n.SettingsAppDataPrefix;
        GuardianDataPrefix = L10n.SettingsGuardianDataPrefix;
        VersionLabel = L10n.SettingsVersionLabel;
        AppVersion = L10n.AppVersion;
    }

    [RelayCommand]
    public void Refresh()
    {
        var appSettings = Services.GetAppSettings();
        var safety = Services.GetSafetySettings();

        SelectedLanguage = appSettings.Language;
        CreateRestoreBeforeBoost = appSettings.CreateRestoreBeforeBoost;
        ConfirmExtremeTweaks = appSettings.ConfirmExtremeTweaks;

        WatcherEnabled = safety.WatcherEnabled;
        BootAutoRevert = safety.BootAutoRevert;
        ConfirmTimerSecs = (int)safety.ConfirmTimerSecs;

        DataDirectory = Services.GetDataDirectory();
        GuardianDirectory = Services.GetGuardianDataDirectory();
    }

    [RelayCommand]
    public void Save()
    {
        var appResult = Services.SaveAppSettings(new AppSettings
        {
            Language = SelectedLanguage,
            CreateRestoreBeforeBoost = CreateRestoreBeforeBoost,
            ConfirmExtremeTweaks = ConfirmExtremeTweaks
        });

        var safetyResult = Services.SaveSafetySettings(new SafetySettings
        {
            Language = SelectedLanguage,
            WatcherEnabled = WatcherEnabled,
            BootAutoRevert = BootAutoRevert,
            ConfirmTimerSecs = (uint)Math.Clamp(ConfirmTimerSecs, 5, 60),
            OnboardingComplete = true
        });

        StatusMessage = $"{appResult.Message} {safetyResult.Message}";
    }
}
