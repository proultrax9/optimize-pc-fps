using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FpsGodPc.Core.Localization;
using FpsGodPc.Core.Services;
using FpsGodPc.Services;
using System.Collections.ObjectModel;

namespace FpsGodPc.App.ViewModels;

public partial class ProfilesPageViewModel(AppServices services, LocalizationService l10n) : PageViewModelBase(services, l10n, "profiles")
{
    // Guards against concurrent full refreshes.
    private bool _isRefreshing;

    // Tracks whether we have loaded profiles at least once.  After the first
    // full load the timer tick only does a cheap watcher-status update instead
    // of re-running the expensive recursive Steam/install detection.
    private bool _profilesLoaded;

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

    /// <summary>
    /// Full refresh: re-runs expensive install detection and reloads all profiles.
    /// Called on first navigation and on explicit user-triggered refresh.
    /// </summary>
    [RelayCommand]
    public async Task Refresh()
    {
        await RefreshFullAsync();
    }

    /// <summary>
    /// Cheap tick update: only refreshes watcher status and applied/active flags
    /// from already-cached profile data without re-running the Steam scan.
    /// Falls back to a full refresh if profiles have never been loaded.
    /// </summary>
    public async Task RefreshTick()
    {
        if (!_profilesLoaded)
        {
            // First tick before load completed — do a full refresh instead.
            await RefreshFullAsync();
            return;
        }

        if (_isRefreshing)
        {
            // A full refresh is already in flight; skip this tick entirely.
            return;
        }

        // Cheap path: update watcher status and the applied/active flags on
        // each already-known profile.  GetWatcherStatus() is a simple DB/memory
        // read — no PowerShell, no recursive filesystem scan.
        await UiDispatch.InvokeAsync(() =>
        {
            UpdateWatcherStatus();
        });
    }

    private async Task RefreshFullAsync()
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

            _profilesLoaded = true;
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
        await RefreshFullAsync();
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
        await RefreshFullAsync();
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
