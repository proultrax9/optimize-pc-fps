using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FpsGodPc.Core.Localization;
using FpsGodPc.Core.Services;
using FpsGodPc.Services;
using System.Collections.ObjectModel;

namespace FpsGodPc.App.ViewModels;

public partial class ProfilesPageViewModel(AppServices services, LocalizationService l10n) : PageViewModelBase(services, l10n, "profiles")
{
    public ObservableCollection<WatcherProfile> Profiles { get; } = [];

    [ObservableProperty]
    private string applyLabel = string.Empty;

    [ObservableProperty]
    private string watcherLabel = string.Empty;

    [ObservableProperty]
    private string watcherStatusText = string.Empty;

    protected override void ApplyPageStrings()
    {
        base.ApplyPageStrings();
        ApplyLabel = L10n.ProfilesApply;
        WatcherLabel = L10n.ProfilesWatcher;
        UpdateWatcherStatus();
    }

    [RelayCommand]
    public void Refresh()
    {
        Profiles.Clear();
        foreach (var profile in Services.ListProfiles())
        {
            Profiles.Add(profile);
        }

        UpdateWatcherStatus();
    }

    [RelayCommand]
    public void ToggleWatcher(string? profileId)
    {
        if (string.IsNullOrWhiteSpace(profileId))
        {
            return;
        }

        var profile = Profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile is null)
        {
            return;
        }

        var updated = !profile.WatcherEnabled;
        StatusMessage = Services.SetProfileWatcher(profileId, updated).Message;
        Refresh();
    }

    [RelayCommand]
    public void ApplyNow(string? profileId)
    {
        if (string.IsNullOrWhiteSpace(profileId))
        {
            return;
        }

        StatusMessage = Services.ApplyProfile(profileId).Message;
        Refresh();
    }

    private void UpdateWatcherStatus()
    {
        var status = Services.GetWatcherStatus();
        if (!string.IsNullOrWhiteSpace(status.ActiveProfileId))
        {
            WatcherStatusText = L10n.WatcherStatusActive(L10n.ProfileName(status.ActiveProfileId!));
        }
        else
        {
            WatcherStatusText = L10n.WatcherStatusIdle;
        }

        if (!string.IsNullOrWhiteSpace(status.LastEvent))
        {
            WatcherStatusText += $" · {L10n.WatcherLastEvent(L10n.LocalizeWatcherEvent(status.LastEvent))}";
        }
    }
}
