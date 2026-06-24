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

        await vm.Refresh();
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _refreshTimer.Tick += async (_, _) => await vm.Refresh();
        _refreshTimer.Start();
    }

    private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
    {
        _refreshTimer?.Stop();
        _refreshTimer = null;
    }
}
