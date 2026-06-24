using System.Windows;
using System.Windows.Threading;
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
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

        try
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
                var shell = sp.GetRequiredService<ShellViewModel>();
                return new BoostPageViewModel(services, l10n, () =>
                {
                    var window = new Windows.ExpertChecklistWindow(services, l10n)
                    {
                        Owner = Current.MainWindow
                    };
                    window.ShowDialog();
                    return window.LastStatusMessage;
                }, key => shell.NavigateCommand.Execute(key));
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

            var main = Services.GetRequiredService<MainWindow>();
            Current.MainWindow = main;
            main.Show();

            _ = InitializeAfterWindowShownAsync(main, appServices, l10n);
        }
        catch (Exception ex)
        {
            ShowFatalError(ex);
            Shutdown(1);
        }
    }

    private static async Task InitializeAfterWindowShownAsync(MainWindow main, AppServices appServices, LocalizationService l10n)
    {
        await Task.Yield();

        try
        {
            var bootNotice = appServices.RunBootSafetyCheck();
            if (!string.IsNullOrWhiteSpace(bootNotice))
            {
                await main.Dispatcher.InvokeAsync(() =>
                    MessageBox.Show(main, bootNotice, l10n.BootSafetyTitle, MessageBoxButton.OK, MessageBoxImage.Warning));
            }

            if (!appServices.GetSafetySettings().OnboardingComplete)
            {
                await main.Dispatcher.InvokeAsync(() =>
                {
                    var onboarding = new Windows.OnboardingWindow(appServices, l10n) { Owner = main };
                    onboarding.ShowDialog();
                });
            }

            appServices.MarkSessionStart();
            appServices.StartBackgroundServices();
        }
        catch (Exception ex)
        {
            await main.Dispatcher.InvokeAsync(() =>
                MessageBox.Show(main, ex.Message, "FPS Optimize GOD PC", MessageBoxButton.OK, MessageBoxImage.Warning));
        }
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        if (Services is null)
        {
            return;
        }

        try
        {
            Services.GetRequiredService<AppServices>().MarkSessionCleanExit();
            Services.GetService<GameWatcherService>()?.Dispose();
        }
        catch
        {
            // Best-effort cleanup on exit.
        }
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ShowFatalError(e.Exception);
        e.Handled = true;
    }

    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            ShowFatalError(ex);
        }
    }

    private static void ShowFatalError(Exception ex)
    {
        try
        {
            MessageBox.Show(
                ex.ToString(),
                "FPS Optimize GOD PC",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
            // UI may not be available.
        }
    }
}
