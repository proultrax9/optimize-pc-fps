using System.Windows.Controls;
using System.Windows.Threading;
using FpsGodPc.App.ViewModels;

namespace FpsGodPc.App.Views;

public partial class ProfilesPage : UserControl
{
    private DispatcherTimer? _refreshTimer;

    public ProfilesPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is not ProfilesPageViewModel vm)
        {
            return;
        }

        try
        {
            // Full refresh (expensive Steam/install detection) on first load.
            await vm.Refresh();
        }
        catch
        {
            // Swallow; VM surfaces error state via StatusMessage.
        }

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _refreshTimer.Tick += async (_, _) =>
        {
            try
            {
                // Cheap tick: only updates watcher status from cache; does NOT
                // re-run the recursive PowerShell Steam scan.
                await vm.RefreshTick();
            }
            catch
            {
                // Swallow so a transient error does not bubble to
                // DispatcherUnhandledException and show a repeated error dialog.
            }
        };
        _refreshTimer.Start();
    }

    private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
    {
        _refreshTimer?.Stop();
        _refreshTimer = null;
    }
}
