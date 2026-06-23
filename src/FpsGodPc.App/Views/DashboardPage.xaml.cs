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

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is not DashboardPageViewModel vm)
        {
            return;
        }

        vm.Refresh();
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _refreshTimer.Tick += (_, _) => vm.Refresh();
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
