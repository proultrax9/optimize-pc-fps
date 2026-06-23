using System.Windows;
using FpsGodPc.App.ViewModels;
using FpsGodPc.Core.Localization;
using FpsGodPc.Core.Services;
using FpsGodPc.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FpsGodPc.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        var collection = new ServiceCollection();
        collection.AddFpsGodPcAppServices();
        collection.AddSingleton<ShellViewModel>();
        collection.AddTransient<DashboardPageViewModel>();
        collection.AddTransient<BenchmarkPageViewModel>();
        collection.AddTransient<ScannerPageViewModel>();
        collection.AddTransient<TweaksPageViewModel>();
        collection.AddTransient<BoostPageViewModel>(sp =>
        {
            var services = sp.GetRequiredService<AppServices>();
            var l10n = sp.GetRequiredService<LocalizationService>();
            return new BoostPageViewModel(services, l10n, () =>
            {
                var window = new Windows.ExpertChecklistWindow(services, l10n)
                {
                    Owner = Current.MainWindow
                };
                window.ShowDialog();
                return window.LastStatusMessage;
            });
        });
        collection.AddTransient<ProfilesPageViewModel>();
        collection.AddTransient<SafetyPageViewModel>();
        collection.AddTransient<CleanerPageViewModel>();
        collection.AddTransient<RestorePageViewModel>();
        collection.AddTransient<GamesPageViewModel>();
        collection.AddTransient<NetworkPageViewModel>();
        collection.AddTransient<SettingsPageViewModel>();
        collection.AddSingleton<MainWindow>();

        Services = collection.BuildServiceProvider();
        var appServices = Services.GetRequiredService<AppServices>();
        var l10n = Services.GetRequiredService<LocalizationService>();
        appServices.MarkSessionStart();
        appServices.StartBackgroundServices();

        var bootNotice = appServices.RunBootSafetyCheck();
        if (!string.IsNullOrWhiteSpace(bootNotice))
        {
            MessageBox.Show(bootNotice, l10n.BootSafetyTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        var main = Services.GetRequiredService<MainWindow>();
        main.Show();

        if (!appServices.GetSafetySettings().OnboardingComplete)
        {
            var onboarding = new Windows.OnboardingWindow(appServices, l10n)
            {
                Owner = main
            };
            onboarding.ShowDialog();
        }
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        var appServices = Services.GetRequiredService<AppServices>();
        appServices.MarkSessionCleanExit();
        Services.GetService<GameWatcherService>()?.Dispose();
    }
}
