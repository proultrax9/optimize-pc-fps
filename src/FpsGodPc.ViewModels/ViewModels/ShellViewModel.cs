using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FpsGodPc.App.Navigation;
using FpsGodPc.Core.Localization;
using FpsGodPc.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace FpsGodPc.App.ViewModels;

public partial class ShellViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AppServices _services;
    private readonly LocalizationService _l10n;
    private readonly Dictionary<string, Func<PageViewModelBase>> _pageFactory;

    public ShellViewModel(IServiceProvider serviceProvider, AppServices services, LocalizationService l10n)
    {
        _serviceProvider = serviceProvider;
        _services = services;
        _l10n = l10n;
        _pageFactory = new Dictionary<string, Func<PageViewModelBase>>(StringComparer.OrdinalIgnoreCase)
        {
            ["dashboard"] = () => _serviceProvider.GetRequiredService<DashboardPageViewModel>(),
            ["benchmark"] = () => _serviceProvider.GetRequiredService<BenchmarkPageViewModel>(),
            ["scanner"] = () => _serviceProvider.GetRequiredService<ScannerPageViewModel>(),
            ["tweaks"] = () => _serviceProvider.GetRequiredService<TweaksPageViewModel>(),
            ["boost"] = () => _serviceProvider.GetRequiredService<BoostPageViewModel>(),
            ["profiles"] = () => _serviceProvider.GetRequiredService<ProfilesPageViewModel>(),
            ["safety"] = () => _serviceProvider.GetRequiredService<SafetyPageViewModel>(),
            ["cleaner"] = () => _serviceProvider.GetRequiredService<CleanerPageViewModel>(),
            ["restore"] = () => _serviceProvider.GetRequiredService<RestorePageViewModel>(),
            ["games"] = () => _serviceProvider.GetRequiredService<GamesPageViewModel>(),
            ["network"] = () => _serviceProvider.GetRequiredService<NetworkPageViewModel>(),
            ["settings"] = () => _serviceProvider.GetRequiredService<SettingsPageViewModel>(),
        };

        _l10n.SetLanguage(_services.GetAppSettings().Language);
        _l10n.LanguageChanged += OnLanguageChanged;

        IsElevated = _services.IsElevated();
        ApplyShellStrings();
        BuildNavigation();
        StartConfirmTimerPolling();
        Navigate("dashboard");
    }

    private DispatcherTimer? _confirmTimer;
    private int _confirmSecondsLeft;
    private bool _confirmCountdownActive;

    public List<NavSectionViewModel> NavigationSections { get; } = [];
    public ObservableCollection<string> Languages { get; } = [ "en", "th" ];

    [ObservableProperty]
    private PageViewModelBase? currentPage;

    [ObservableProperty]
    private bool isElevated;

    [ObservableProperty]
    private string languageLabel = string.Empty;

    [ObservableProperty]
    private string dismissWatchdogLabel = string.Empty;

    private string _selectedLanguage = string.Empty;
    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (!SetProperty(ref _selectedLanguage, value) || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var settings = _services.GetAppSettings();
            settings.Language = value;
            _services.SaveAppSettings(settings);
            _l10n.SetLanguage(value);
        }
    }

    [ObservableProperty]
    private string adminBannerText = string.Empty;

    [ObservableProperty]
    private string restartAdminLabel = string.Empty;

    private string? _watchdogMessage;
    public string? WatchdogMessage
    {
        get => _watchdogMessage;
        set
        {
            if (SetProperty(ref _watchdogMessage, value))
            {
                HasWatchdogMessage = !string.IsNullOrWhiteSpace(value);
            }
        }
    }

    [ObservableProperty]
    private bool hasWatchdogMessage;

    [ObservableProperty]
    private bool hasConfirmTimer;

    [ObservableProperty]
    private string confirmTimerTitle = string.Empty;

    [ObservableProperty]
    private string confirmTimerMessage = string.Empty;

    [ObservableProperty]
    private string confirmTimerKeepLabel = string.Empty;

    [ObservableProperty]
    private string confirmTimerRevertLabel = string.Empty;

    [RelayCommand]
    public void Navigate(string? key)
    {
        if (string.IsNullOrWhiteSpace(key) || !_pageFactory.TryGetValue(key, out var factory))
        {
            return;
        }

        var page = factory();
        CurrentPage = page;

        foreach (var nav in NavigationSections.SelectMany(s => s.Items))
        {
            nav.IsSelected = string.Equals(nav.Key, key, StringComparison.OrdinalIgnoreCase);
        }

        switch (page)
        {
            case DashboardPageViewModel vm: vm.Refresh(); break;
            case BenchmarkPageViewModel vm: vm.Refresh(); break;
            case ScannerPageViewModel vm: vm.RunScan(); break;
            case TweaksPageViewModel vm: vm.Refresh(); break;
            case BoostPageViewModel vm: vm.Refresh(); break;
            case ProfilesPageViewModel vm: vm.Refresh(); break;
            case SafetyPageViewModel vm: vm.Refresh(); break;
            case RestorePageViewModel vm: vm.Refresh(); break;
            case GamesPageViewModel vm: vm.Refresh(); break;
            case SettingsPageViewModel vm: vm.Refresh(); break;
        }
    }

    [RelayCommand]
    public void RestartElevated()
    {
        try
        {
            _services.RestartElevated();
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, _l10n.T("Elevation Failed", "ยกระดับสิทธิ์ไม่สำเร็จ"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public void DismissWatchdog()
    {
        _services.DismissCrashWatchdog();
        WatchdogMessage = null;
    }

    [RelayCommand]
    public void KeepPendingTweaks()
    {
        _services.ConfirmPendingRevert();
        ResetConfirmTimer();
    }

    [RelayCommand]
    public void RejectPendingTweaks()
    {
        _services.RejectPendingRevert();
        ResetConfirmTimer();
    }

    private void StartConfirmTimerPolling()
    {
        _confirmTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _confirmTimer.Tick += (_, _) => TickConfirmTimer();
        _confirmTimer.Start();
    }

    private void TickConfirmTimer()
    {
        var safety = _services.GetSafetyStatus();
        if (!safety.PendingRevert)
        {
            ResetConfirmTimer();
            return;
        }

        if (!_confirmCountdownActive)
        {
            _confirmCountdownActive = true;
            _confirmSecondsLeft = (int)_services.GetSafetySettings().ConfirmTimerSecs;
            HasConfirmTimer = true;
            ConfirmTimerTitle = _l10n.ConfirmTimerTitle;
            ConfirmTimerKeepLabel = _l10n.PendingRevertKeep;
            ConfirmTimerRevertLabel = _l10n.PendingRevertRevert;
        }

        ConfirmTimerMessage = string.IsNullOrWhiteSpace(safety.PendingReason)
            ? $"{_l10n.ConfirmTimerHint} · {_l10n.ConfirmTimerCountdown(_confirmSecondsLeft)}"
            : $"{_l10n.LocalizePendingReason(safety.PendingReason)} · {_l10n.ConfirmTimerCountdown(_confirmSecondsLeft)}";

        if (_confirmSecondsLeft <= 0)
        {
            _services.RejectPendingRevert();
            ResetConfirmTimer();
            return;
        }

        _confirmSecondsLeft--;
    }

    private void ResetConfirmTimer()
    {
        _confirmCountdownActive = false;
        HasConfirmTimer = false;
        ConfirmTimerMessage = string.Empty;
    }

    private void OnLanguageChanged()
    {
        ApplyShellStrings();
        BuildNavigation();
    }

    private void ApplyShellStrings()
    {
        AdminBannerText = _l10n.AdminBanner();
        RestartAdminLabel = _l10n.RestartAsAdmin();
        LanguageLabel = _l10n.ShellLanguage;
        DismissWatchdogLabel = _l10n.DismissWatchdog;
        WatchdogMessage = _services.GetCrashWatchdogMessage() is not null ? _l10n.WatchdogMessage() : null;
        SelectedLanguage = _l10n.Language;
    }
    private void BuildNavigation()
    {
        var selected = NavigationSections.SelectMany(s => s.Items).FirstOrDefault(i => i.IsSelected)?.Key;
        NavigationSections.Clear();
        var order = new[] { "MONITOR", "PERFORMANCE", "OPTIMIZE", "TOOLS" };
        var grouped = NavigationCatalog.Items.Where(i => i.Section != "FOOTER").GroupBy(i => i.Section);

        foreach (var section in order)
        {
            var group = grouped.FirstOrDefault(g => string.Equals(g.Key, section, StringComparison.OrdinalIgnoreCase));
            if (group is null) continue;

            NavigationSections.Add(new NavSectionViewModel(
                _l10n.Section(group.Key),
                group.Select(i => new NavEntryViewModel(i.Key, _l10n.Nav(i.Key)))));
        }

        var footer = NavigationCatalog.Items.Where(i => i.Section == "FOOTER").ToList();
        if (footer.Count > 0)
        {
            NavigationSections.Add(new NavSectionViewModel(
                string.Empty,
                footer.Select(i => new NavEntryViewModel(i.Key, _l10n.Nav(i.Key))),
                isFooter: true));
        }

        if (!string.IsNullOrWhiteSpace(selected))
        {
            foreach (var nav in NavigationSections.SelectMany(s => s.Items))
            {
                nav.IsSelected = string.Equals(nav.Key, selected, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
