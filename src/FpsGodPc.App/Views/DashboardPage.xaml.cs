using System.Windows.Controls;
using System.Windows.Threading;
using FpsGodPc.App.ViewModels;

namespace FpsGodPc.App.Views;

public partial class DashboardPage : UserControl
{
    private DispatcherTimer? _refreshTimer;

    public DashboardPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is not DashboardPageViewModel vm)
        {
            return;
        }

        try
        {
            await vm.Refresh();
        }
        catch
        {
            // Swallow exceptions from the initial load; the VM exposes
            // StatusMessage/error state to the UI directly.
        }

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _refreshTimer.Tick += async (_, _) =>
        {
            try
            {
                await vm.Refresh();
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
        if (_refreshTimer is null)
        {
            return;
        }

        _refreshTimer.Stop();
        _refreshTimer = null;
    }
}
