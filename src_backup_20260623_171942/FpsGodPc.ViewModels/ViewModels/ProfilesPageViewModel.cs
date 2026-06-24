using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FpsGodPc.Core.Localization;
using FpsGodPc.Core.Services;
using FpsGodPc.Services;
using System.Collections.ObjectModel;

namespace FpsGodPc.App.ViewModels;

public partial class ProfilesPageViewModel(AppServices services, LocalizationService l10n) : PageViewModelBase(services, l10n, "profiles")
{
    private bool _isRefreshing;

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
    public async Task Refresh()
    {
        if (_isRefreshing)
        {
            return;
        }

        _isRefreshing = true;
        try
        {
            var profileData = await Task.Run(() => Services.ListProfiles());

            await UiDispatch.InvokeAsync(() =>
            {
                Profiles.Clear();
                foreach (var profile in profileData)
                {
                    Profiles.Add(profile);
                }

                UpdateWatcherStatus();
            });
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    [RelayCommand]
    public async Task ToggleWatcher(string? profileId)
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
        var result = await Task.Run(() => Services.SetProfileWatcher(profileId, updated));
        StatusMessage = result.Message;
        await Refresh();
    }

    [RelayCommand]
    public async Task ApplyNow(string? profileId)
    {
        if (string.IsNullOrWhiteSpace(profileId))
        {
            return;
        }

        var result = await Task.Run(() => Services.ApplyProfile(profileId));
        StatusMessage = result.Message;
        await Refresh();
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
