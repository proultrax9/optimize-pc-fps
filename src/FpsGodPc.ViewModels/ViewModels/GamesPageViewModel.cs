using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FpsGodPc.Core.Localization;
using FpsGodPc.Core.Services;
using FpsGodPc.Core.Models;
using System.Collections.ObjectModel;

namespace FpsGodPc.App.ViewModels;

public partial class GamesPageViewModel(AppServices services, LocalizationService l10n) : PageViewModelBase(services, l10n, "games")
{
    private bool _isRefreshing;

    public ObservableCollection<GameProfile> Games { get; } = [];
    public ObservableCollection<string> GameNotes { get; } = [];

    private GameProfile? _selectedGame;
    public GameProfile? SelectedGame
    {
        get => _selectedGame;
        set
        {
            if (SetProperty(ref _selectedGame, value))
            {
                ShowSelectHint = value is null;
                UpdateInstallStatus();
                UpdateGameDetails();
            }
        }
    }

    [ObservableProperty]
    private string fpsCapPrefix = string.Empty;

    [ObservableProperty]
    private string priorityPrefix = string.Empty;

    [ObservableProperty]
    private string launchOptionsPrefix = string.Empty;

    [ObservableProperty]
    private string selectHint = string.Empty;

    [ObservableProperty]
    private string applyProfileLabel = string.Empty;

    [ObservableProperty]
    private string installStatus = string.Empty;

    [ObservableProperty]
    private string priorityDisplay = "—";

    [ObservableProperty]
    private bool showSelectHint = true;

    [ObservableProperty]
    private bool isBusy;

    protected override void ApplyPageStrings()
    {
        base.ApplyPageStrings();
        FpsCapPrefix = L10n.GamesFpsCapPrefix;
        PriorityPrefix = L10n.GamesPriorityPrefix;
        LaunchOptionsPrefix = L10n.GamesLaunchOptionsPrefix;
        SelectHint = L10n.GamesSelectHint;
        ApplyProfileLabel = L10n.GamesApplyProfile;
        UpdateInstallStatus();
        UpdateGameDetails();
    }

    [RelayCommand]
    public async Task Refresh()
    {
        if (_isRefreshing)
        {
            return;
        }

        _isRefreshing = true;
        IsBusy = true;
        try
        {
            var profiles = await Task.Run(() => Services.GetGameProfiles());

            await UiDispatch.InvokeAsync(() =>
            {
                Games.Clear();
                foreach (var game in profiles)
                {
                    Games.Add(game);
                }

                SelectedGame ??= Games.FirstOrDefault();
                ShowSelectHint = SelectedGame is null;
                UpdateInstallStatus();
                UpdateGameDetails();
            });
        }
        finally
        {
            _isRefreshing = false;
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void Select(string? gameId)
    {
        if (string.IsNullOrWhiteSpace(gameId))
        {
            return;
        }

        SelectedGame = Games.FirstOrDefault(g => g.Id == gameId);
    }

    [RelayCommand]
    public async Task ApplyProfile()
    {
        if (SelectedGame is null)
        {
            return;
        }

        var gameId = SelectedGame.Id;
        var result = await Task.Run(() => Services.ApplyProfile(gameId));
        StatusMessage = result.Message;
        await Refresh();
    }

    private void UpdateInstallStatus()
    {
        InstallStatus = SelectedGame?.Installed == true ? L10n.GamesInstalled : L10n.GamesNotInstalled;
    }

    private void UpdateGameDetails()
    {
        PriorityDisplay = L10n.GamePriority(SelectedGame?.Priority);
        GameNotes.Clear();
        if (SelectedGame is null)
        {
            return;
        }

        foreach (var note in L10n.GameNotesForProfile(SelectedGame.Id))
        {
            GameNotes.Add(note);
        }
    }
}
